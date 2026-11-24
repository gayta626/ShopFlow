

using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public interface IProductImageRepository
{
    public Task<List<ProductImage>> GetByProductIdAsync(int productId);

    public Task<ProductImage?> GetByIdAsync(int productId, int imageId);

    public void Add(ProductImage image);

    public void Remove(ProductImage image);

    public Task<int> SaveChangeAsync();
}