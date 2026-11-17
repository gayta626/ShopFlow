

using Microsoft.EntityFrameworkCore.Storage;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdForUserAsync(int id, int userId);

    public Task<IDbContextTransaction> BeginTransactionAsync();

    public Task<Cart?> GetCartForCheckoutAsync(int userId);

    public void Add(Order order);

    public void RemoveCartItems(IEnumerable<CartItem> items);

    public Task<int> SaveChangeAsync();

    public Task<int> CountForUserAsync(int userId, OrderStatus? status);

    public Task<List<Order>> GetPageForUserAsync(int userId, int page, int size, OrderStatus? status);

    public Task<Order?> GetOrderForUpdateAsync(int orderId, int userId);

    public Task<Order?> GetOrderForAdminUpdateAsync(int orderId);
}
