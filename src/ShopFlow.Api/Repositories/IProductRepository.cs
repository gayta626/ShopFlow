

using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public interface IProductRepository
{
    public Task<bool> GetBySkuAsync(string sku);

    public void Add(Product product);

    public Task<int> SaveChangAsync();

    public Task<Product?> GetByIdAsync(int id);

    public Task<int> CountAsync();

    public Task<List<Product>> GetPageAsync(int page, int size);

    public Task<List<Product>> GetActivePageAsync(int page, int size, int? categoryId, string? query,
        decimal? minPrice, decimal? maxPrice, string sort, SwitchType? switchType, Connectivity? connectivity);

    public Task<int> CountActiveAsync(int? categoryId, string? query, decimal? minPrice, decimal? maxPrice,
        SwitchType? switchType, Connectivity? connectivity);

    public Task<Product?> GetByIdForUpdateAsync(int id);

    public Task<bool> IsSkuExited(string sku, int productId);

    public void SetOriginalRowVersion(Product product, uint rowVersion);
}
