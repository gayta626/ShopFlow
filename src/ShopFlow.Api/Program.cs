using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens; // Cung cấp TokenValidationParameters và SymmetricSecurityKey để kiểm tra JWT.
using System.Security.Claims; // Cung cấp ClaimTypes để chỉ định claim chứa role.
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ShopFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter(allowIntegerValues: false));
    });
// Cấu hình cách xác thực JWT do ShopFlow cấp.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme) // Chọn Bearer làm cách xác thực mặc định.
    .AddJwtBearer(options => // options là đối tượng cấu hình; phần trong { } thiết lập cách kiểm tra token.
    {
        var keyBase64 = builder.Configuration["Jwt:Key"] // Đọc cùng key Base64 mà JwtTokenService dùng để ký token.
            ?? throw new InvalidOperationException("Thiếu cấu hình JWT key."); // Dừng với lỗi rõ ràng nếu không có key.
        var keyBytes = Convert.FromBase64String(keyBase64); // Chuyển chuỗi Base64 thành byte giống lúc ký.
        if (keyBytes.Length < 32) // HS256 cần key ít nhất 32 byte.
            throw new InvalidOperationException("JWT key phải có ít nhất 32 byte."); // Không chấp nhận key quá ngắn.

        options.MapInboundClaims = false; // Giữ nguyên tên claim, ví dụ sub, khi đưa vào User.
        options.TokenValidationParameters = new TokenValidationParameters // Tạo bộ quy tắc kiểm tra token.
        {
            ValidateIssuerSigningKey = true, // Bật kiểm tra signing key.
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes), // Dùng key dạng byte để kiểm tra chữ ký.
            ValidAlgorithms = new[] { SecurityAlgorithms.HmacSha256 }, // Chỉ chấp nhận thuật toán HS256 giống bên cấp token.
            ValidateIssuer = true, // Kiểm tra bên phát hành token.
            ValidIssuer = builder.Configuration["Jwt:Issuer"], // Issuer phải khớp cấu hình dùng lúc cấp token.
            ValidateAudience = true, // Kiểm tra đối tượng nhận token.
            ValidAudience = builder.Configuration["Jwt:Audience"], // Audience phải khớp cấu hình dùng lúc cấp token.
            ValidateLifetime = true, // Kiểm tra thời gian hiệu lực của token.
            RequireExpirationTime = true, // Bắt buộc token phải có thời điểm hết hạn.
            RequireSignedTokens = true, // Bắt buộc token phải được ký.
            ClockSkew = TimeSpan.Zero, // Không cộng thời gian dung sai khi kiểm tra hạn token.
            NameClaimType = "sub", // Dùng sub (UserId) làm User.Identity.Name.
            RoleClaimType = ClaimTypes.Role // Đọc role từ đúng loại claim mà JwtTokenService đã tạo.
        }; // Kết thúc bộ quy tắc kiểm tra.
    }); // Kết thúc cấu hình Bearer và lời gọi đăng ký dịch vụ.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ProductsService>();
var app = builder.Build();

if (args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ShopFlowDbContext>();
    var savedEntries = await DbSeeder.SeedAsync(db);
    Console.WriteLine(savedEntries == 0
        ? "Không có thay đổi: dữ liệu mẫu đã tồn tại, không ghi đè."
        : $"Seed hoàn tất: đã lưu {savedEntries} bản ghi mới.");

    return;
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication(); // Kiểm tra token và đưa danh tính vào User trước khi kiểm tra quyền.
app.UseAuthorization(); // Kiểm tra User có đáp ứng yêu cầu truy cập endpoint hay không.

app.MapControllers();

app.Run();
