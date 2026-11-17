

using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public interface ICartRepository
{
    public Task<Cart?> GetByUserIdAsync(int userId);
    public Task<Cart?> GetByUserIdForUpdateAsync(int userId);

    public Task<int> SaveChangeAsync();
    public void RemoveItem(CartItem item);

}