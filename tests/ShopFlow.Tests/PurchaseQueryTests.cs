using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Dtos.Carts;
using ShopFlow.Api.Dtos.Products;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Tests;

[Collection("PostgreSQL")]
public class PurchaseQueryTests
{
    [Fact]
    public async Task CustomerOrders_FilterCountAndStablePagesRespectOwnership()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(customers: 2, quantity: 0);
        var time = DateTimeOffset.UtcNow;
        int[] expected;
        await using (var setup = data.Open())
        {
            var orders = new[]
            {
                new Order { UserId = data.Users[0].Id, Status = OrderStatus.Pending, TotalAmount = 100, ShippingAddress = "Test address", CreatedAt = time },
                new Order { UserId = data.Users[0].Id, Status = OrderStatus.Paid, TotalAmount = 100, ShippingAddress = "Test address", CreatedAt = time },
                new Order { UserId = data.Users[0].Id, Status = OrderStatus.Pending, TotalAmount = 100, ShippingAddress = "Test address", CreatedAt = time },
                new Order { UserId = data.Users[1].Id, Status = OrderStatus.Pending, TotalAmount = 100, ShippingAddress = "Test address", CreatedAt = time }
            };
            setup.Orders.AddRange(orders);
            await setup.SaveChangesAsync();
            expected = orders.Where(o => o.UserId == data.Users[0].Id && o.Status == OrderStatus.Pending)
                .OrderByDescending(o => o.Id).Select(o => o.Id).ToArray();
        }
        await using var db = data.Open();
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead);
        var service = PurchaseTestData.Orders(db);
        var all = await service.GetListForUserAsync(data.Users[0].Id, 1, 20, null);
        Assert.Equal(3, all.TotalCount);
        var first = await service.GetListForUserAsync(data.Users[0].Id, 1, 1, OrderStatus.Pending);
        var second = await service.GetListForUserAsync(data.Users[0].Id, 2, 1, OrderStatus.Pending);
        var empty = await service.GetListForUserAsync(data.Users[0].Id, 3, 1, OrderStatus.Pending);
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, second.TotalCount);
        Assert.Equal(expected, new[] { Assert.Single(first.Items).Id, Assert.Single(second.Items).Id });
        Assert.Empty(empty.Items);
        Assert.Equal(2, empty.TotalCount);
        // Admin có thể thấy dữ liệu từ test khác: đối chiếu toàn bộ truy vấn hiện tại của DB.
        var expectedAdminCount = await db.Orders.CountAsync(o => o.Status == OrderStatus.Pending);
        var expectedPage = await db.Orders.AsNoTracking().Where(o => o.Status == OrderStatus.Pending)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id).Take(2).Select(o => o.Id).ToListAsync();
        var admin = await service.GetListForAdminAsync(1, 2, OrderStatus.Pending);
        Assert.Equal(expectedAdminCount, admin.TotalCount);
        Assert.Equal(expectedPage, admin.Items.Select(o => o.Id));
    }

    [Fact]
    public async Task CatalogFiltersBeforePaging_IncludesOutOfStockButNotInactive()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(quantity: 0, productCount: 4);
        await using (var setup = data.Open())
        {
            var ids = data.Products.Select(p => p.Id).ToArray();
            var products = await setup.Products.Where(p => ids.Contains(p.Id)).OrderBy(p => p.Id).ToListAsync();
            products[0].Stock = 0;
            products[0].Connectivity = Connectivity.Wired | Connectivity.Bluetooth;
            products[1].Connectivity = Connectivity.Bluetooth;
            products[1].Price = products[0].Price;
            products[2].IsActive = false;
            await setup.SaveChangesAsync();
        }
        await using var db = data.Open();
        var query = new ProductQueryRequest
        {
            CategoryId = data.Products[0].CategoryId, SwitchType = SwitchType.Linear,
            Connectivity = Connectivity.Bluetooth, MinPrice = 100000, MaxPrice = 100000,
            Sort = "priceAsc", Size = 1
        };
        var service = PurchaseTestData.Catalog(db);
        var first = await service.GetPublicListAsync(query);
        query.Page = 2;
        var second = await service.GetPublicListAsync(query);
        query.Page = 3;
        var empty = await service.GetPublicListAsync(query);
        Assert.Equal(2, first.TotalCount);
        Assert.Equal(2, second.TotalCount);
        Assert.Equal(data.Products[0].Id, Assert.Single(first.Items).Id);
        Assert.Equal(data.Products[1].Id, Assert.Single(second.Items).Id);
        Assert.Empty(empty.Items);
        query = new ProductQueryRequest { CategoryId = data.Products[0].CategoryId, Sort = "priceDesc" };
        var active = await service.GetPublicListAsync(query);
        Assert.Equal(3, active.TotalCount);
        Assert.DoesNotContain(active.Items, p => p.Id == data.Products[2].Id);
        Assert.Equal(data.Products[3].Id, active.Items[0].Id);
        query.Q = "  " + data.Products[0].Sku + "  ";
        var bySku = await service.GetPublicListAsync(query);
        Assert.Equal(data.Products[0].Id, Assert.Single(bySku.Items).Id);
        query.Q = data.Products[1].Name;
        Assert.Equal(data.Products[1].Id, Assert.Single((await service.GetPublicListAsync(query)).Items).Id);
    }

    [Fact]
    public async Task CartAddMergeUpdateDelete_EnforcesQuantityAndStockWithoutReservingStock()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(stock: 100, quantity: 0);
        var userId = data.Users[0].Id;
        var productId = data.Products[0].Id;
        await using (var db = data.Open())
        {
            var service = PurchaseTestData.Carts(db);
            await service.AddItemAsync(userId, new AddCartItemRequest { ProductId = productId, Quantity = 50 });
            var merged = await service.AddItemAsync(userId, new AddCartItemRequest { ProductId = productId, Quantity = 49 });
            Assert.Equal(99, Assert.Single(merged!.Items).Quantity);
            await Assert.ThrowsAsync<CartRuleException>(() => service.AddItemAsync(userId,
                new AddCartItemRequest { ProductId = productId, Quantity = 1 }));
        }
        await using (var db = data.Open())
        {
            var service = PurchaseTestData.Carts(db);
            var updated = await service.UpdateAsync(productId, userId, new UpdateCartItemRequest { Quantity = 2 });
            Assert.Equal(2, Assert.Single(updated!.Items).Quantity);
            Assert.Empty((await service.DeleteAsync(productId, userId))!.Items);
        }
        await using var verify = data.Open();
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == userId);
        Assert.Empty(cart.Items);
        Assert.Equal(4L, cart.Revision);
        Assert.Equal(100L, (await verify.Products.SingleAsync(p => p.Id == productId)).Stock);
    }
}
