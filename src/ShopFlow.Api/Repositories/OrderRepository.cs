

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public class OrderRepository : IOrderRepository
{

    private readonly ShopFlowDbContext _db;


    public OrderRepository(ShopFlowDbContext db)
    {
        _db = db;
    }
    public async Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return await _db.Database.BeginTransactionAsync();
    }

    public async Task<Cart?> GetCartForCheckoutAsync(int userId)
    {
        var cart = await _db.Carts.FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return null;

        await _db.Entry(cart)
            .Collection(c => c.Items)
            .Query()
            .Include(item => item.Product)
            .LoadAsync();

        return cart;
    }

    public void RemoveCartItems(IEnumerable<CartItem> items)
    {
        _db.CartItems.RemoveRange(items);
    }

    public async Task<int> SaveChangeAsync()
    {
        return await _db.SaveChangesAsync();
    }

    public void Add(Order order)
    {
        _db.Orders.Add(order);
    }

    public Task<Order?> GetByIdForUserAsync(int id, int userId)
    {
        return _db.Orders.AsNoTracking()
            .Include(order => order.Items)
            .FirstOrDefaultAsync(order => order.Id == id && order.UserId == userId);
    }
    private IQueryable<Order> BuildUserOrdersQuery(int userId, OrderStatus? status)
    {
        var query = _db.Orders
            .AsNoTracking()
            .Where(order => order.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(order => order.Status == status.Value);
        }

        return query;
    }
    public async Task<int> CountForUserAsync(int userId, OrderStatus? status)
    {
        return await BuildUserOrdersQuery(userId, status).CountAsync();
    }

    public async Task<List<Order>> GetPageForUserAsync(int userId, int page, int size, OrderStatus? status)
    {
        return await BuildUserOrdersQuery(userId, status)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();
    }

    public async Task<Order?> GetOrderForUpdateAsync(int orderId, int userId)
    {
        var order = await _db.Orders.Include(order => order.Items)
            .ThenInclude(order => order.Product)
            .Where(order => order.Id == orderId)
            .Where(order => order.UserId == userId)
            .FirstOrDefaultAsync();

        if (order == null) return null;
        return order;
    }

    public async Task<Order?> GetOrderForAdminUpdateAsync(int orderId)
    {
        var order = await _db.Orders.Include(order => order.Items)
            .ThenInclude(order => order.Product)
            .Where(order => order.Id == orderId)
            .FirstOrDefaultAsync();

        if (order == null) return null;
        return order;
    }
}
