using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShopFlow.Api.Data;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Products;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;

namespace ShopFlow.Tests;

internal sealed class PurchaseTestData : IAsyncDisposable
{
    private readonly string _suffix = Guid.NewGuid().ToString("N");
    public List<User> Users { get; } = [];
    public List<Product> Products { get; } = [];
    public static CheckoutRequest Address => new() { ShippingAddress = "123 Nguyễn Trãi, Quận 5, TP.HCM" };

    public ShopFlowDbContext Open(params IInterceptor[] interceptors)
        => new(new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only")
            .AddInterceptors(interceptors).Options);

    public static OrderService Orders(ShopFlowDbContext db) => new(new OrderRepository(db));
    public static ProductsService Catalog(ShopFlowDbContext db) => new(new ProductRepository(db), new CategoryRepository(db));
    public static CartService Carts(ShopFlowDbContext db) => new(new CartRepository(db), new ProductRepository(db));

    public async Task SeedAsync(int customers = 1, long stock = 10, int quantity = 2, int productCount = 1)
    {
        await using var db = Open();
        await db.Database.MigrateAsync();
        var category = new Category { Name = "Purchase-" + _suffix, NormalizedName = "PURCHASE-" + _suffix };
        for (var i = 0; i < productCount; i++)
        {
            var product = new Product
            {
                Sku = $"BUY-{_suffix}-{i}".ToUpperInvariant(), Name = "Keyboard " + i,
                Brand = "Demo", Color = "Black", Category = category,
                SwitchType = SwitchType.Linear, Connectivity = Connectivity.Wired,
                IsHotSwappable = true, IsActive = true, Stock = stock, Price = 100000m * (i + 1)
            };
            Products.Add(product);
            db.Products.Add(product);
        }
        for (var i = 0; i < customers; i++)
        {
            var email = $"purchase-{_suffix}-{i}@example.com";
            var user = new User
            {
                Email = email, NormalizedEmail = email.ToUpperInvariant(),
                FullName = "Purchase test", Role = UserRole.Customer, PasswordHash = "unused-test-hash"
            };
            user.Cart = new Cart { User = user };
            if (quantity > 0)
                foreach (var product in Products)
                    user.Cart.Items.Add(new CartItem { Product = product, Quantity = quantity });
            Users.Add(user);
            db.Users.Add(user);
        }
        await db.SaveChangesAsync();
    }

    public async Task<int> CheckoutAsync()
    {
        await using var db = Open();
        var response = await Orders(db).CheckoutAsync(Users[0].Id, Address);
        Assert.NotNull(response);
        return response.Id;
    }

    public static UpdateProductRequest Edit(Product p) => new()
    {
        Sku = p.Sku, Name = p.Name, CategoryId = p.CategoryId, Brand = p.Brand,
        Color = p.Color, SwitchType = p.SwitchType, Connectivity = [Connectivity.Wired],
        IsHotSwappable = p.IsHotSwappable, Description = p.Description, Price = p.Price,
        Stock = p.Stock, IsActive = p.IsActive, RowVersion = p.RowVersion
    };

    public async ValueTask DisposeAsync()
    {
        await using var db = Open();
        var emails = Users.Select(u => u.NormalizedEmail).ToArray();
        var orders = await db.Orders.Include(o => o.Items).Where(o => emails.Contains(o.User.NormalizedEmail)).ToListAsync();
        var carts = await db.Carts.Include(c => c.Items).Where(c => emails.Contains(c.User.NormalizedEmail)).ToListAsync();
        db.OrderItems.RemoveRange(orders.SelectMany(o => o.Items));
        db.Orders.RemoveRange(orders);
        db.CartItems.RemoveRange(carts.SelectMany(c => c.Items));
        db.Carts.RemoveRange(carts);
        var skus = Products.Select(p => p.Sku).ToArray();
        db.Products.RemoveRange(await db.Products.Where(p => skus.Contains(p.Sku)).ToListAsync());
        db.Categories.RemoveRange(await db.Categories.Where(c => c.NormalizedName == "PURCHASE-" + _suffix).ToListAsync());
        db.Users.RemoveRange(await db.Users.Where(u => emails.Contains(u.NormalizedEmail)).ToListAsync());
        await db.SaveChangesAsync();
    }
}

// Chờ ngay trước SQL ghi: mọi service đã đọc dữ liệu và đều quyết định thử lưu.
internal sealed class PurchaseSaveGate(int participants) : SaveChangesInterceptor
{
    private int _arrivals;
    private readonly TaskCompletionSource<bool> _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    public int Arrivals => Volatile.Read(ref _arrivals);

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _arrivals) == participants) _ready.TrySetResult(true);
        await _ready.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        return result;
    }
}

internal sealed class InjectedPurchaseFailure : Exception;

// SaveChanges đã thực thi SQL nhưng transaction của checkout/cancel chưa commit.
internal sealed class FailAfterPurchaseSave : SaveChangesInterceptor
{
    public bool Reached { get; private set; }
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Assert.NotNull(eventData.Context!.Database.CurrentTransaction);
        Reached = true;
        throw new InjectedPurchaseFailure();
    }
}
