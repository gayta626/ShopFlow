

using Microsoft.EntityFrameworkCore;
using Npgsql;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Data;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ShopFlowDbContext _db;

    public CartRepository(ShopFlowDbContext db)
    {
        _db = db;
    }

    public async Task<Cart?> GetByUserIdAsync(int userId)
    {
        var cart = await _db.Carts.AsNoTracking()
            .Include(c => c.Items)
            .ThenInclude(c => c.Product)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null) return null;
        return cart;
    }

    public async Task<Cart?> GetByUserIdForUpdateAsync(int userId)
    {
        var cart = await _db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.UserId == userId);
        if (cart == null) return null;
        return cart;
    }

    public async Task<int> SaveChangeAsync()
    {
        try
        {
            return await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new CartConcurrencyException(ex);
        }
        // Hai request cùng thêm một sản phẩm mới có thể chạm unique trước Revision.
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgres &&
            postgres.SqlState == PostgresErrorCodes.UniqueViolation &&
            postgres.ConstraintName == "IX_CartItems_CartId_ProductID")
        {
            throw new CartConcurrencyException(ex);
        }
    }

    public void RemoveItem(CartItem item)
    {
        _db.CartItems.Remove(item);
    }
}
