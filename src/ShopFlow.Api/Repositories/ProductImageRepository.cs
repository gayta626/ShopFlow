

using System.Net;
using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public class ProductImageRepository : IProductImageRepository
{
    private readonly ShopFlowDbContext _db;

    public ProductImageRepository(ShopFlowDbContext db)
    {
        _db = db;
    }
    public async Task<List<ProductImage>> GetByProductIdAsync(int productId)
    {
        var images = await _db.ProductImages.AsNoTracking()
            .Where(image => image.ProductId == productId)
            .OrderBy(image => image.SortOrder)
            .ThenBy(image => image.Id)
            .ToListAsync();
        return images;
    }

    public async Task<ProductImage?> GetByIdAsync(int productId, int imageId)
    {
        var image = await _db.ProductImages
            .Where(image => image.ProductId == productId && image.Id == imageId)
            .FirstOrDefaultAsync();

        return image;
    }

    public void Add(ProductImage image)
    {
        _db.ProductImages.Add(image);
    }

    public void Remove(ProductImage image)
    {
        _db.ProductImages.Remove(image);
    }

    public async Task<int> SaveChangeAsync()
    {
        return await _db.SaveChangesAsync();
    }
}