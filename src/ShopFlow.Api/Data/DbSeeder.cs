using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Data;

public static class DbSeeder
{
    public static async Task<int> SeedAsync(ShopFlowDbContext db)
    {
        const string categoryName = "75%";
        const string sku = "DEMO75-BLK-LIN";

        var category = await db.Categories.SingleOrDefaultAsync(c => c.NormalizedName == categoryName);

        if (category == null)
        {
            category = new Category
            {
                Name = categoryName,
                NormalizedName = categoryName
            };
            db.Categories.Add(category);
        }

        var productExists = await db.Products.AnyAsync(p => p.Sku == sku);

        if (!productExists)
        {
            var product = new Product
            {
                Sku = sku,
                Name = "Ban phim mau 75%",
                Brand = "Demo",
                Color = "Black",
                SwitchType = SwitchType.Linear,
                Connectivity = Connectivity.Wired,
                IsHotSwappable = true,
                Description = "Du lieu gia dung de hoc va kiem thu.",
                Price = 1500000m,
                Stock = 10,
                Category = category
            };
            db.Products.Add(product);
        }
        // Tra ve so entity EF thuc su luu; 0 nghia la khong co thay doi.
        return await db.SaveChangesAsync();
    }
}
