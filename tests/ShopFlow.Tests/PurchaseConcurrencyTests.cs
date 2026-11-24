using Microsoft.EntityFrameworkCore;
using ShopFlow.Api.Dtos.Carts;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Tests;

[Collection("PostgreSQL")]
public class PurchaseConcurrencyTests
{
    [Fact]
    public async Task CancelVersusPaid_OnlyOneCommits_StockMatchesWinner()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var orderId = await data.CheckoutAsync();
        var gate = new PurchaseSaveGate(2);
        await using var cancelDb = data.Open(gate);
        await using var paidDb = data.Open(gate);
        var errors = await Task.WhenAll(
            Record.ExceptionAsync(() => PurchaseTestData.Orders(cancelDb).CancelForUserAsync(orderId, data.Users[0].Id)),
            Record.ExceptionAsync(() => PurchaseTestData.Orders(paidDb).UpdateStatusForAdminAsync(orderId,
                new UpdateOrderStatusRequest { Status = OrderStatus.Paid })));
        Assert.Equal(2, gate.Arrivals);
        Assert.Single(errors, e => e is null);
        Assert.IsType<OrderConcurrencyException>(Assert.Single(errors, e => e is not null));
        await using var verify = data.Open();
        var order = await verify.Orders.SingleAsync(o => o.Id == orderId);
        var product = await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id);
        if (errors[0] is null)
        {
            Assert.Equal(OrderStatus.Cancelled, order.Status);
            Assert.NotNull(order.CancelledAt);
            Assert.Null(order.PaidAt);
            Assert.Equal(10L, product.Stock);
        }
        else
        {
            Assert.Equal(OrderStatus.Paid, order.Status);
            Assert.NotNull(order.PaidAt);
            Assert.Null(order.CancelledAt);
            Assert.Equal(8L, product.Stock);
            await Assert.ThrowsAsync<OrderRuleException>(() => PurchaseTestData.Orders(verify)
                .CancelForUserAsync(orderId, data.Users[0].Id));
        }
    }

    [Fact]
    public async Task TenCustomersBuyLastItem_OneOrderAndNineConflicts()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(customers: 10, stock: 1, quantity: 1);
        var gate = new PurchaseSaveGate(10);
        var errors = await Task.WhenAll(data.Users.Select(async user =>
        {
            await using var db = data.Open(gate);
            return await Record.ExceptionAsync(() => PurchaseTestData.Orders(db).CheckoutAsync(user.Id, PurchaseTestData.Address));
        }));
        Assert.Equal(10, gate.Arrivals);
        Assert.Single(errors, e => e is null);
        Assert.Equal(9, errors.Count(e => e is OrderConcurrencyException));
        await using var verify = data.Open();
        var ids = data.Users.Select(u => u.Id).ToArray();
        var order = Assert.Single(await verify.Orders.Where(o => ids.Contains(o.UserId)).ToListAsync());
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(0L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
        var carts = await verify.Carts.Include(c => c.Items).Where(c => ids.Contains(c.UserId)).ToListAsync();
        foreach (var cart in carts)
        {
            Assert.Equal(cart.UserId == order.UserId ? 1L : 0L, cart.Revision);
            if (cart.UserId == order.UserId) Assert.Empty(cart.Items);
            else Assert.Equal(1, Assert.Single(cart.Items).Quantity);
        }
    }

    [Fact]
    public async Task TwoCheckoutsOfSameCart_CreateOnlyOneOrder()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var gate = new PurchaseSaveGate(2);
        var errors = await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ =>
        {
            await using var db = data.Open(gate);
            return await Record.ExceptionAsync(() => PurchaseTestData.Orders(db)
                .CheckoutAsync(data.Users[0].Id, PurchaseTestData.Address));
        }));
        Assert.Equal(2, gate.Arrivals);
        Assert.Single(errors, e => e is null);
        Assert.IsType<OrderConcurrencyException>(Assert.Single(errors, e => e is not null));
        await using var verify = data.Open();
        Assert.Single(await verify.Orders.Where(o => o.UserId == data.Users[0].Id).ToListAsync());
        Assert.Equal(8L, (await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id)).Stock);
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        Assert.Empty(cart.Items);
        Assert.Equal(1L, cart.Revision);
    }

    [Fact]
    public async Task CartUpdateVersusCheckout_DoesNotLoseCommittedChange()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var gate = new PurchaseSaveGate(2);
        await using var checkoutDb = data.Open(gate);
        await using var editDb = data.Open(gate);
        var errors = await Task.WhenAll(
            Record.ExceptionAsync(() => PurchaseTestData.Orders(checkoutDb).CheckoutAsync(data.Users[0].Id, PurchaseTestData.Address)),
            Record.ExceptionAsync(() => PurchaseTestData.Carts(editDb).UpdateAsync(data.Products[0].Id,
                data.Users[0].Id, new UpdateCartItemRequest { Quantity = 3 })));
        Assert.Equal(2, gate.Arrivals);
        Assert.Single(errors, e => e is null);
        await using var verify = data.Open();
        var orders = await verify.Orders.Include(o => o.Items).Where(o => o.UserId == data.Users[0].Id).ToListAsync();
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        var product = await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id);
        Assert.Equal(1L, cart.Revision);
        if (errors[0] is null)
        {
            Assert.IsType<CartConcurrencyException>(errors[1]);
            Assert.Equal(2, Assert.Single(Assert.Single(orders).Items).Quantity);
            Assert.Empty(cart.Items);
            Assert.Equal(8L, product.Stock);
        }
        else
        {
            Assert.IsType<OrderConcurrencyException>(errors[0]);
            Assert.Empty(orders);
            Assert.Equal(3, Assert.Single(cart.Items).Quantity);
            Assert.Equal(10L, product.Stock);
        }
    }

    [Fact]
    public async Task ConfigurationEditVersusCheckout_OnlyOneCommits()
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync();
        var gate = new PurchaseSaveGate(2);
        await using var checkoutDb = data.Open(gate);
        await using var editDb = data.Open(gate);
        var request = PurchaseTestData.Edit(data.Products[0]);
        request.Color = "White";
        var errors = await Task.WhenAll(
            Record.ExceptionAsync(() => PurchaseTestData.Orders(checkoutDb).CheckoutAsync(data.Users[0].Id, PurchaseTestData.Address)),
            Record.ExceptionAsync(() => PurchaseTestData.Catalog(editDb).UpdateAsync(data.Products[0].Id, request)));
        Assert.Equal(2, gate.Arrivals);
        Assert.Single(errors, e => e is null);
        await using var verify = data.Open();
        var product = await verify.Products.SingleAsync(p => p.Id == data.Products[0].Id);
        var orders = await verify.Orders.Where(o => o.UserId == data.Users[0].Id).ToListAsync();
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        if (errors[0] is null)
        {
            Assert.IsType<ProductConcurrencyException>(errors[1]);
            Assert.Single(orders);
            Assert.Equal("Black", product.Color);
            Assert.Equal(8L, product.Stock);
            Assert.Empty(cart.Items);
            Assert.Equal(1L, cart.Revision);
        }
        else
        {
            Assert.IsType<OrderConcurrencyException>(errors[0]);
            Assert.Empty(orders);
            Assert.Equal("White", product.Color);
            Assert.Equal(10L, product.Stock);
            Assert.Equal(2, Assert.Single(cart.Items).Quantity);
            Assert.Equal(0L, cart.Revision);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailureAfterSaveBeforeCommit_RollsBackEntirePurchase(bool cancel)
    {
        await using var data = new PurchaseTestData();
        await data.SeedAsync(productCount: 2);
        var orderId = cancel ? await data.CheckoutAsync() : 0;
        var fault = new FailAfterPurchaseSave();
        await using (var db = data.Open(fault))
        {
            await Assert.ThrowsAsync<InjectedPurchaseFailure>(async () =>
            {
                if (cancel) await PurchaseTestData.Orders(db).CancelForUserAsync(orderId, data.Users[0].Id);
                else await PurchaseTestData.Orders(db).CheckoutAsync(data.Users[0].Id, PurchaseTestData.Address);
            });
        }
        Assert.True(fault.Reached);
        await using var verify = data.Open();
        var ids = data.Products.Select(p => p.Id).ToArray();
        var products = await verify.Products.Where(p => ids.Contains(p.Id)).ToListAsync();
        Assert.All(products, p => Assert.Equal(cancel ? 8L : 10L, p.Stock));
        var orders = await verify.Orders.Include(o => o.Items).Where(o => o.UserId == data.Users[0].Id).ToListAsync();
        var cart = await verify.Carts.Include(c => c.Items).SingleAsync(c => c.UserId == data.Users[0].Id);
        if (cancel)
        {
            var order = Assert.Single(orders);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Null(order.CancelledAt);
            Assert.Equal(2, order.Items.Count);
            Assert.Empty(cart.Items);
            Assert.Equal(1L, cart.Revision);
        }
        else
        {
            Assert.Empty(orders);
            Assert.False(await verify.OrderItems.AnyAsync(i => ids.Contains(i.ProductId)));
            Assert.Equal(2, cart.Items.Count);
            Assert.All(cart.Items, i => Assert.Equal(2, i.Quantity));
            Assert.Equal(0L, cart.Revision);
        }
    }
}
