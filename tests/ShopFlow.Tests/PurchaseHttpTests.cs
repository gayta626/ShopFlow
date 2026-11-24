using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Services;

namespace ShopFlow.Tests;

// Chạy API thật ở cổng loopback ngẫu nhiên, chỉ dùng DB và khóa JWT dành cho test.
public sealed class PurchaseApiHost : IAsyncLifetime
{
    private Process? _process;
    private readonly byte[] _key = RandomNumberGenerator.GetBytes(32);
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await using (var data = new PurchaseTestData())
        await using (var db = data.Open())
            await db.Database.MigrateAsync();
        var ready = new TaskCompletionSource<Uri>(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = AppContext.BaseDirectory
        };
        start.ArgumentList.Add(Path.Combine(AppContext.BaseDirectory, "ShopFlow.Api.dll"));
        start.Environment["ASPNETCORE_URLS"] = "http://127.0.0.1:0";
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";
        start.Environment["ASPNETCORE_HTTPS_PORT"] = "";
        start.Environment["ConnectionStrings__DefaultConnection"] =
            "Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only";
        start.Environment["Jwt__Key"] = Convert.ToBase64String(_key);
        start.Environment["Jwt__Issuer"] = "PurchaseTests";
        start.Environment["Jwt__Audience"] = "PurchaseTests";
        start.Environment["Jwt__ExpirationMinutes"] = "10";
        start.Environment["Logging__LogLevel__Default"] = "Warning";
        start.Environment["Logging__LogLevel__Microsoft.Hosting.Lifetime"] = "Information";
        _process = new Process { StartInfo = start, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) =>
        {
            const string marker = "Now listening on: ";
            var index = e.Data?.IndexOf(marker, StringComparison.Ordinal) ?? -1;
            if (index >= 0 && Uri.TryCreate(e.Data![(index + marker.Length)..].Trim(), UriKind.Absolute, out var uri))
                ready.TrySetResult(uri);
        };
        _process.ErrorDataReceived += (_, _) => { };
        _process.Exited += (_, _) => ready.TrySetException(new InvalidOperationException("Test API exited before becoming ready."));
        try
        {
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
            Client = new HttpClient { BaseAddress = await ready.Task.WaitAsync(TimeSpan.FromSeconds(30)), Timeout = TimeSpan.FromSeconds(20) };
        }
        catch
        {
            await DisposeAsync();
            throw;
        }
    }

    public string Token(int userId, string role = "Customer", bool expired = false)
    {
        if (expired)
            return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                issuer: "PurchaseTests", audience: "PurchaseTests",
                claims: [new Claim("sub", userId.ToString()), new Claim(ClaimTypes.Role, role)],
                expires: DateTime.UtcNow.AddMinutes(-1),
                signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256)));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Issuer"] = "PurchaseTests", ["Jwt:Audience"] = "PurchaseTests",
            ["Jwt:Key"] = Convert.ToBase64String(_key), ["Jwt:ExpirationMinutes"] = "10"
        }).Build();
        return new JwtTokenService(config).CreateToken(new User { Id = userId, Role = Enum.Parse<UserRole>(role) }).AccessToken;
    }

    public async Task<HttpResponseMessage> Send(HttpMethod method, string path, string? token = null, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null) request.Content = JsonContent.Create(body);
        return await Client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_process is not null)
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                await _process.WaitForExitAsync();
            }
            _process.Dispose();
        }
    }
}

[Collection("PostgreSQL")]
public class PurchaseHttpTests(PurchaseApiHost host) : IClassFixture<PurchaseApiHost>
{
    [Theory]
    [InlineData("missing")]
    [InlineData("invalid")]
    [InlineData("expired")]
    public async Task ProtectedOrdersRejectInvalidAuthentication(string kind)
    {
        var token = kind == "missing" ? null : kind == "invalid" ? "invalid" : host.Token(1, expired: true);
        using var response = await host.Send(HttpMethod.Get, "/api/orders/me", token);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("GET", "/api/admin/orders", "Customer")]
    [InlineData("GET", "/api/admin/orders/1", "Customer")]
    [InlineData("PUT", "/api/admin/orders/1/status", "Customer")]
    [InlineData("POST", "/api/orders/1/complete", "Admin")]
    [InlineData("POST", "/api/orders/1/cancel", "Admin")]
    public async Task WrongRoleGetsForbidden(string method, string path, string role)
    {
        using var response = await host.Send(new HttpMethod(method), path, host.Token(1, role),
            method == "PUT" ? new { status = "Paid" } : null);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutLocationOwnershipAndLifecycle_WorkThroughHttp()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(customers: 2);
        var owner = host.Token(data.Users[0].Id);
        var other = host.Token(data.Users[1].Id);
        var admin = host.Token(data.Users[0].Id, "Admin");
        using var created = await host.Send(HttpMethod.Post, "/api/orders", owner, PurchaseTestData.Address);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.NotNull(created.Headers.Location);
        var json = await created.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetInt32();
        Assert.Equal(200000m, json.GetProperty("totalAmount").GetDecimal());
        Assert.EndsWith($"/api/orders/{id}", created.Headers.Location.ToString());
        using var fetched = await host.Send(HttpMethod.Get, created.Headers.Location.ToString(), owner);
        Assert.Equal(HttpStatusCode.OK, fetched.StatusCode);
        foreach (var path in new[] { $"/api/orders/{id}", $"/api/orders/{id}/cancel", $"/api/orders/{id}/complete" })
        {
            using var denied = await host.Send(path.EndsWith(id.ToString()) ? HttpMethod.Get : HttpMethod.Post, path, other);
            Assert.Equal(HttpStatusCode.NotFound, denied.StatusCode);
        }
        using var adminDetail = await host.Send(HttpMethod.Get, $"/api/admin/orders/{id}", admin);
        Assert.Equal(HttpStatusCode.OK, adminDetail.StatusCode);
        using var missing = await host.Send(HttpMethod.Get, "/api/admin/orders/2147483647", admin);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using var premature = await host.Send(HttpMethod.Post, $"/api/orders/{id}/complete", owner);
        Assert.Equal(HttpStatusCode.Conflict, premature.StatusCode);
        foreach (var status in new[] { "Paid", "Shipped" })
        {
            using var updated = await host.Send(HttpMethod.Put, $"/api/admin/orders/{id}/status", admin, new { status });
            Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        }
        using var adminComplete = await host.Send(HttpMethod.Put, $"/api/admin/orders/{id}/status", admin, new { status = "Completed" });
        Assert.Equal(HttpStatusCode.Conflict, adminComplete.StatusCode);
        using var completed = await host.Send(HttpMethod.Post, $"/api/orders/{id}/complete", owner);
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        using var repeated = await host.Send(HttpMethod.Post, $"/api/orders/{id}/complete", owner);
        Assert.Equal(HttpStatusCode.OK, repeated.StatusCode);
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
        Assert.NotNull(order.PaidAt);
        Assert.NotNull(order.ShippedAt);
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("size=101")]
    [InlineData("page=2147483647&size=100")]
    [InlineData("status=999")]
    [InlineData("status=invalid")]
    public async Task InvalidOrderQueryReturnsBadRequest(string query)
    {
        using var customer = await host.Send(HttpMethod.Get, "/api/orders/me?" + query, host.Token(1));
        using var admin = await host.Send(HttpMethod.Get, "/api/admin/orders?" + query, host.Token(1, "Admin"));
        Assert.Equal(HttpStatusCode.BadRequest, customer.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, admin.StatusCode);
    }

    [Theory]
    [InlineData("page=0")]
    [InlineData("size=101")]
    [InlineData("minPrice=200&maxPrice=100")]
    [InlineData("sort=invalid")]
    [InlineData("switchType=999")]
    [InlineData("connectivity=999")]
    public async Task InvalidCatalogQueryReturnsBadRequest(string query)
    {
        using var response = await host.Send(HttpMethod.Get, "/api/products?" + query);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CheckoutAndCancelReturnExpectedErrorsAndIdempotentSuccess()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(stock: 1);
        var token = host.Token(data.Users[0].Id);
        using var invalid = await host.Send(HttpMethod.Post, "/api/orders", token, new { shippingAddress = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using var shortage = await host.Send(HttpMethod.Post, "/api/orders", token, PurchaseTestData.Address);
        Assert.Equal(HttpStatusCode.Conflict, shortage.StatusCode);
        Assert.Equal("INSUFFICIENT_STOCK", (await shortage.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        await using (var edit = data.Open())
        {
            var product = await edit.Products.SingleAsync(p => p.Id == data.Products[0].Id);
            product.Stock = 10;
            await edit.SaveChangesAsync();
        }
        using var created = await host.Send(HttpMethod.Post, "/api/orders", token, PurchaseTestData.Address);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        using var empty = await host.Send(HttpMethod.Post, "/api/orders", token, PurchaseTestData.Address);
        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        for (var i = 0; i < 2; i++)
        {
            using var cancelled = await host.Send(HttpMethod.Post, $"/api/orders/{id}/cancel", token);
            Assert.Equal(HttpStatusCode.OK, cancelled.StatusCode);
        }
        await using var verify = data.Open();
        Assert.Equal(10L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
        Assert.Equal(OrderStatus.Cancelled, (await verify.Orders.SingleAsync(o => o.Id == id)).Status);
    }
}
