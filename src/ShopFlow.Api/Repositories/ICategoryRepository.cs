
using Microsoft.EntityFrameworkCore.Storage;
using ShopFlow.Api.Entities;
namespace ShopFlow.Api.Repositories;


public interface ICategoryRepository
{
    public Task<List<Category>> GetAllAsync();

    public Task<Category?> GetByIdAsync(int id);

    public Task<Category?> GetByNormalizedNameAsync(string normalizedName);

    public void Add(Category category);

    public Task<int> SaveChangeAsync();

    public Task<bool> HasProductAsync(int cateId);

    public Task<IDbContextTransaction> BeginTransactionAsync();

    public void Remove(Category category);

}
