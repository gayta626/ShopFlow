using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Dtos.Carts;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Tests;

[Collection("PostgreSQL")]
public class PurchaseRulesTests
{
    [Fact]
    public async Task CheckoutMultipleItems_PreservesSnapshotAfterCatalogChanges()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(productCount: 2);
        var id = await data.CheckoutAsync();
        await using (var edit = data.Open())
        {
            var product = await edit.Products.SingleAsync(p => p.Id == data.Products[0].Id);
            var request = PurchaseTestData.Edit(product);
            request.Name = "Renamed keyboard";
            request.Price = 900000m;
            await PurchaseTestData.Catalog(edit).UpdateAsync(product.Id, request);
        }
        await using var verify = data.Open();
        var response = await PurchaseTestData.Orders(verify).GetByIdAsync(id, data.Users[0].Id);
        Assert.NotNull(response);
        Assert.Equal(600000m, response.TotalAmount);
        Assert.Equal(2, response.Items.Count);
        foreach (var original in data.Products)
        {
            var item = Assert.Single(response.Items, i => i.ProductId == original.Id);
            Assert.Equal(original.Sku, item.Sku);
            Assert.Equal(original.Name, item.ProductName);
            Assert.Equal(original.Price, item.UnitPrice);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(original.Price * 2, item.LineTotal);
            Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == original.Id)).Stock);
        }
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        Assert.Empty(cart.Items);
        Assert.Equal(1L, cart.Revision);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckoutUnavailableItem_DoesNotChangeAnyProductOrCart(bool inactive)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(productCount: 2);
        await using (var edit = data.Open())
        {
            var product = await edit.Products.SingleAsync(p => p.Id == data.Products[1].Id);
            if (inactive) product.IsActive = false;
            else product.Stock = 1;
            await edit.SaveChangesAsync();
        }
        await using (var db = data.Open())
        {
            var error = await Assert.ThrowsAsync<OrderRuleException>(() => PurchaseTestData.Orders(db)
                .CheckoutAsync(data.Users[0].Id, PurchaseTestData.Address));
            Assert.Equal(inactive ? "PRODUCT_UNAVAILABLE" : "INSUFFICIENT_STOCK", error.Code);
        }
        await using var verify = data.Open();
        Assert.False(await verify.Orders.AnyAsync(o => o.UserId == data.Users[0].Id));
        Assert.Equal(10L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
        Assert.Equal(inactive ? 10L : 1L, (await verify.Products.SingleAsync(p => p.Id == data.Products[1].Id)).Stock);
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        Assert.Equal(0L, cart.Revision);
        Assert.Equal(2, cart.Items.Count);
        Assert.All(cart.Items, i => Assert.Equal(2, i.Quantity));
    }

    [Fact]
    public async Task OtherCustomerCannotReadCancelOrCompleteOrder()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(customers: 2);
        var id = await data.CheckoutAsync();
        await using (var db = data.Open())
        {
            var service = PurchaseTestData.Orders(db);
            Assert.Null(await service.GetByIdAsync(id, data.Users[1].Id));
            Assert.Null(await service.CancelForUserAsync(id, data.Users[1].Id));
            Assert.Null(await service.CompleteForUserAsync(id, data.Users[1].Id));
        }
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Null(order.CancelledAt);
        Assert.Null(order.CompletedAt);
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Paid)]
    [InlineData(OrderStatus.Pending, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Paid, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Paid, OrderStatus.Cancelled)]
    public async Task AdminValidTransition_RepeatPreservesStockAndTimestamp(OrderStatus start, OrderStatus target)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await SetStatusAsync(data, id, start);
        await using (var db = data.Open())
            Assert.Equal(target.ToString(), (await PurchaseTestData.Orders(db).UpdateStatusForAdminAsync(id,
                new UpdateOrderStatusRequest { Status = target }))!.Status);
        await using var before = data.Open();
        var first = await before.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
        Assert.NotNull(target == OrderStatus.Paid ? first.PaidAt : target == OrderStatus.Shipped ? first.ShippedAt : first.CancelledAt);
        await using (var repeat = data.Open())
            await PurchaseTestData.Orders(repeat).UpdateStatusForAdminAsync(id, new UpdateOrderStatusRequest { Status = target });
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.Equal(target, order.Status);
        Assert.Equal(first.PaidAt, order.PaidAt);
        Assert.Equal(first.ShippedAt, order.ShippedAt);
        Assert.Equal(first.CancelledAt, order.CancelledAt);
        Assert.Equal(first.RowVersion, order.RowVersion);
        Assert.Equal(target == OrderStatus.Cancelled ? 10L : 8L,
            (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData(OrderStatus.Pending, OrderStatus.Shipped)]
    [InlineData(OrderStatus.Pending, OrderStatus.Completed)]
    [InlineData(OrderStatus.Paid, OrderStatus.Pending)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Completed)]
    [InlineData(OrderStatus.Shipped, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Completed, OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Cancelled, OrderStatus.Paid)]
    public async Task AdminInvalidTransition_LeavesDatabaseUnchanged(OrderStatus start, OrderStatus target)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await SetStatusAsync(data, id, start);
        await using (var db = data.Open())
            await Assert.ThrowsAsync<OrderRuleException>(() => PurchaseTestData.Orders(db)
                .UpdateStatusForAdminAsync(id, new UpdateOrderStatusRequest { Status = target }));
        await using var verify = data.Open();
        Assert.Equal(start, (await verify.Orders.SingleAsync(o => o.Id == id)).Status);
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Shipped)]
    [InlineData(OrderStatus.Completed)]
    public async Task CustomerCannotCancelAfterPending(OrderStatus status)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await SetStatusAsync(data, id, status);
        await using (var db = data.Open())
            await Assert.ThrowsAsync<OrderRuleException>(() => PurchaseTestData.Orders(db).CancelForUserAsync(id, data.Users[0].Id));
        await using var verify = data.Open();
        Assert.Equal(status, (await verify.Orders.SingleAsync(o => o.Id == id)).Status);
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomerCancel_RepeatedCallDoesNotRestockAgain_EvenAboveAdminLimit(bool replenish)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        if (replenish)
        {
            await using var edit = data.Open();
            var product = await edit.Products.SingleAsync(p => p.Id == data.Products[0].Id);
            var request = PurchaseTestData.Edit(product);
            request.Stock = 1000000;
            await PurchaseTestData.Catalog(edit).UpdateAsync(product.Id, request);
        }
        await using (var db = data.Open())
            await PurchaseTestData.Orders(db).CancelForUserAsync(id, data.Users[0].Id);
        await using var firstDb = data.Open();
        var first = await firstDb.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
        await using (var repeat = data.Open())
            Assert.Equal("Cancelled", (await PurchaseTestData.Orders(repeat).CancelForUserAsync(id, data.Users[0].Id))!.Status);
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.NotNull(order.CancelledAt);
        Assert.Equal(first.CancelledAt, order.CancelledAt);
        Assert.Equal(first.RowVersion, order.RowVersion);
        Assert.Equal(replenish ? 1000002L : 10L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Fact]
    public async Task CustomerCompletesShippedOrder_RepeatKeepsTimestampAndStock()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await SetStatusAsync(data, id, OrderStatus.Shipped);
        await using (var db = data.Open())
            Assert.Equal("Completed", (await PurchaseTestData.Orders(db).CompleteForUserAsync(id, data.Users[0].Id))!.Status);
        await using var firstDb = data.Open();
        var first = await firstDb.Orders.AsNoTracking().SingleAsync(o => o.Id == id);
        await using (var repeat = data.Open())
            await PurchaseTestData.Orders(repeat).CompleteForUserAsync(id, data.Users[0].Id);
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.NotNull(order.CompletedAt);
        Assert.Equal(first.CompletedAt, order.CompletedAt);
        Assert.Equal(first.RowVersion, order.RowVersion);
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Paid)]
    [InlineData(OrderStatus.Cancelled)]
    public async Task CompleteBeforeShipping_IsRejected(OrderStatus status)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await SetStatusAsync(data, id, status);
        await using (var db = data.Open())
            await Assert.ThrowsAsync<OrderRuleException>(() => PurchaseTestData.Orders(db).CompleteForUserAsync(id, data.Users[0].Id));
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == id);
        Assert.Equal(status, order.Status);
        Assert.Null(order.CompletedAt);
    }

    [Fact]
    public async Task StaleAdminStockUpdateAfterCheckout_IsRejected()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var request = PurchaseTestData.Edit(data.Products[0]);
        request.Stock = 20;
        await data.CheckoutAsync();
        await using (var db = data.Open())
            await Assert.ThrowsAsync<ProductConcurrencyException>(() => PurchaseTestData.Catalog(db).UpdateAsync(data.Products[0].Id, request));
        await using var verify = data.Open();
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
    }

    [Theory]
    [InlineData("sku")]
    [InlineData("category")]
    [InlineData("brand")]
    [InlineData("color")]
    [InlineData("switch")]
    [InlineData("connectivity")]
    [InlineData("hotswap")]
    public async Task ConfigurationRemainsLockedEvenAfterCancellation(string field)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var id = await data.CheckoutAsync();
        await using (var cancel = data.Open())
            await PurchaseTestData.Orders(cancel).CancelForUserAsync(id, data.Users[0].Id);
        // Một fixture riêng cung cấp CategoryId hợp lệ để kiểm tra đúng khóa cấu hình.
        await using var other = new PurchaseTestData();
        await other.SeedAsync(quantity: 0);
        await using (var db = data.Open())
        {
            var product = await db.Products.SingleAsync(p => p.Id == data.Products[0].Id);
            var request = PurchaseTestData.Edit(product);
            switch (field)
            {
                case "sku": request.Sku += "-NEW"; break;
                case "category": request.CategoryId = other.Products[0].CategoryId; break;
                case "brand": request.Brand = "Other"; break;
                case "color": request.Color = "White"; break;
                case "switch": request.SwitchType = SwitchType.Unknown; break;
                case "connectivity": request.Connectivity = [Connectivity.Bluetooth]; break;
                case "hotswap": request.IsHotSwappable = false; break;
            }
            await Assert.ThrowsAsync<ProductConfigurationLockedException>(() => PurchaseTestData.Catalog(db).UpdateAsync(product.Id, request));
        }
        await using var verify = data.Open();
        var saved = await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id);
        var original = data.Products[0];
        Assert.Equal(original.Sku, saved.Sku);
        Assert.Equal(original.CategoryId, saved.CategoryId);
        Assert.Equal(original.Brand, saved.Brand);
        Assert.Equal(original.Color, saved.Color);
        Assert.Equal(original.SwitchType, saved.SwitchType);
        Assert.Equal(original.Connectivity, saved.Connectivity);
        Assert.Equal(original.IsHotSwappable, saved.IsHotSwappable);
        Assert.Equal(10L, saved.Stock);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(-1)]
    public async Task InvalidCartQuantity_IsRejectedWithoutChanges(int quantity)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        await using (var db = data.Open())
        {
            await Assert.ThrowsAsync<ArgumentException>(() => PurchaseTestData.Carts(db).UpdateAsync(data.Products[0].Id,
                data.Users[0].Id, new UpdateCartItemRequest { Quantity = quantity }));
            await Assert.ThrowsAsync<ArgumentException>(() => PurchaseTestData.Carts(db).AddItemAsync(data.Users[0].Id,
                new AddCartItemRequest { ProductId = data.Products[0].Id, Quantity = quantity }));
        }
        await using var verify = data.Open();
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        Assert.Equal(0L, cart.Revision);
        Assert.Equal(2, Assert.Single(cart.Items).Quantity);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EmptyCartOrInvalidAddress_DoesNotCreateOrder(bool emptyCart)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(quantity: emptyCart ? 0 : 2);
        await using (var db = data.Open())
            await Assert.ThrowsAsync<ArgumentException>(() => PurchaseTestData.Orders(db).CheckoutAsync(data.Users[0].Id,
                emptyCart ? PurchaseTestData.Address : new CheckoutRequest { ShippingAddress = "short" }));
        await using var verify = data.Open();
        Assert.False(await verify.Orders.AnyAsync(o => o.UserId == data.Users[0].Id));
        Assert.Equal(10L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        Assert.Equal(0L, cart.Revision);
        Assert.Equal(emptyCart ? 0 : 1, cart.Items.Count);
    }

    private static async Task SetStatusAsync(PurchaseTestData data, int id, OrderStatus status)
    {
        // Chuẩn bị trạng thái đầu vào riêng cho mỗi case; chuyển trạng thái thật được kiểm tra ở phần Act.
        await using var db = data.Open();
        var order = await db.Orders.SingleAsync(o => o.Id == id);
        order.Status = status;
        await db.SaveChangesAsync();
    }
}
