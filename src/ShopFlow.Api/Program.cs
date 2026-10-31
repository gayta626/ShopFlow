using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ShopFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

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

app.UseAuthorization();

app.MapControllers();

app.Run();
