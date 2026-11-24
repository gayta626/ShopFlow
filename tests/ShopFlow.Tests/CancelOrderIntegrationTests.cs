using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopFlow.Api.Data;
using ShopFlow.Api.Dtos.Orders;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Exceptions;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;

namespace ShopFlow.Tests;

[Collection("PostgreSQL")]
public class CancelOrderIntegrationTests
{
    [Fact]
    public async Task CancelForUserAsync_WhenSameOrderIsCancelledConcurrently_RestocksOnlyOnce()
    {
        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only")
            .Options;

        await using var setupDb = new ShopFlowDbContext(options);
        await setupDb.Database.MigrateAsync();

        var suffix = Guid.NewGuid().ToString("N");
        var category = new Category
        {
            Name = "Cancel-" + suffix,
            NormalizedName = "CANCEL-" + suffix
        };
        var product = new Product
        {
            Sku = "CANCEL-" + suffix,
            Name = "Keyboard cancel test",
            Brand = "Demo",
            Color = "Black",
            SwitchType = SwitchType.Linear,
            Connectivity = Connectivity.Wired,
            IsHotSwappable = true,
            Price = 100000m,
            // Giả lập sau checkout: đã bán 2 chiếc, kho còn 8.
            Stock = 8,
            IsActive = true,
            Category = category
        };
        var email = "cancel-" + suffix + "@example.com";
        var user = new User
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            FullName = "Cancel test customer",
            Role = UserRole.Customer,
            PasswordHash = "unused-in-cancel-test"
        };
        var cart = new Cart { User = user, Revision = 1 };
        user.Cart = cart;
        var order = new Order
        {
            User = user,
            Status = OrderStatus.Pending,
            ShippingAddress = "123 Nguyễn Trãi, Quận 5, TP.HCM",
            TotalAmount = 200000m
        };
        order.Items.Add(new OrderItem
        {
            Product = product,
            Sku = product.Sku,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = 2
        });
        setupDb.Orders.Add(order);

        try
        {
            await setupDb.SaveChangesAsync();
            await using var firstDb = new ShopFlowDbContext(options);
            await using var secondDb = new ShopFlowDbContext(options);
            var gate = new CancelGate();
            var firstRepository = new CoordinatedCancelRepository(new OrderRepository(firstDb), gate);
            var secondRepository = new CoordinatedCancelRepository(new OrderRepository(secondDb), gate);
            var firstService = new OrderService(firstRepository);
            var secondService = new OrderService(secondRepository);
            OrderResponse? firstResponse = null;
            OrderResponse? secondResponse = null;

            // Khởi động cả hai trước khi await để cùng đi đến cổng sau khi đọc đơn.
            var firstAttempt = Record.ExceptionAsync(async () =>
            {
                firstResponse = await firstService.CancelForUserAsync(order.Id, user.Id);
            });
            var secondAttempt = Record.ExceptionAsync(async () =>
            {
                secondResponse = await secondService.CancelForUserAsync(order.Id, user.Id);
            });
            var errors = await Task.WhenAll(firstAttempt, secondAttempt);

            Assert.Equal(2, gate.Arrivals);
            foreach (var repository in new[] { firstRepository, secondRepository })
            {
                Assert.Equal(OrderStatus.Pending, repository.StatusAtRead);
                Assert.Equal(order.RowVersion, repository.OrderVersionAtRead);
                Assert.Equal(product.RowVersion, repository.ProductVersionAtRead);
                Assert.Equal(8L, repository.StockAtRead);
                Assert.Equal(1, repository.SaveAttempts);
            }
            Assert.Single(errors, error => error is null);
            Assert.IsType<OrderConcurrencyException>(Assert.Single(errors, error => error is not null));
            var winner = errors[0] is null ? firstResponse : secondResponse;
            Assert.NotNull(winner);
            Assert.Null(errors[0] is null ? secondResponse : firstResponse);
            Assert.Equal(order.Id, winner.Id);
            Assert.Equal("Cancelled", winner.Status);

            // Đọc bằng context mới để kiểm tra dữ liệu đã commit, không dùng bộ nhớ của hai lượt hủy.
            await using var verificationDb = new ShopFlowDbContext(options);
            var savedOrder = await verificationDb.Orders.AsNoTracking().Include(o => o.Items)
                .SingleAsync(o => o.Id == order.Id);
            var savedProduct = await verificationDb.Products.AsNoTracking()
                .SingleAsync(p => p.Id == product.Id);
            Assert.Equal(OrderStatus.Cancelled, savedOrder.Status);
            Assert.NotNull(savedOrder.CancelledAt);
            Assert.Equal(10L, savedProduct.Stock);
            Assert.Equal(200000m, savedOrder.TotalAmount);
            Assert.Equal(2, Assert.Single(savedOrder.Items).Quantity);
            var savedCart = await verificationDb.Carts.AsNoTracking().Include(c => c.Items)
                .SingleAsync(c => c.UserId == user.Id);
            Assert.Empty(savedCart.Items);
            Assert.Equal(1L, savedCart.Revision);
        }
        finally
        {
            // Chỉ dọn dữ liệu riêng của lần chạy này, kể cả khi Assert thất bại.
            await using var cleanupDb = new ShopFlowDbContext(options);
            var orders = await cleanupDb.Orders.Include(o => o.Items)
                .Where(o => o.User.NormalizedEmail == user.NormalizedEmail).ToListAsync();
            var carts = await cleanupDb.Carts.Include(c => c.Items)
                .Where(c => c.User.NormalizedEmail == user.NormalizedEmail).ToListAsync();
            cleanupDb.OrderItems.RemoveRange(orders.SelectMany(o => o.Items));
            cleanupDb.Orders.RemoveRange(orders);
            cleanupDb.CartItems.RemoveRange(carts.SelectMany(c => c.Items));
            cleanupDb.Carts.RemoveRange(carts);
            cleanupDb.Products.RemoveRange(await cleanupDb.Products.Where(p => p.Sku == product.Sku).ToListAsync());
            cleanupDb.Categories.RemoveRange(await cleanupDb.Categories
                .Where(c => c.NormalizedName == category.NormalizedName).ToListAsync());
            cleanupDb.Users.RemoveRange(await cleanupDb.Users
                .Where(u => u.NormalizedEmail == user.NormalizedEmail).ToListAsync());
            await cleanupDb.SaveChangesAsync();
        }
    }

    private sealed class CancelGate
    {
        private int _arrivals;
        public int Arrivals => Volatile.Read(ref _arrivals);
        private readonly TaskCompletionSource<bool> _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task WaitForBothAsync()
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
                _bothReady.TrySetResult(true);
            await _bothReady.Task.WaitAsync(TimeSpan.FromSeconds(20));
        }
    }

    private sealed class CoordinatedCancelRepository(OrderRepository inner, CancelGate gate) : IOrderRepository
    {
        public OrderStatus? StatusAtRead { get; private set; }
        public uint? OrderVersionAtRead { get; private set; }
        public uint? ProductVersionAtRead { get; private set; }
        public long? StockAtRead { get; private set; }
        public int SaveAttempts { get; private set; }

        public async Task<Order?> GetOrderForUpdateAsync(int orderId, int userId)
        {
            var order = await inner.GetOrderForUpdateAsync(orderId, userId);
            var product = order?.Items.SingleOrDefault()?.Product;
            StatusAtRead = order?.Status;
            OrderVersionAtRead = order?.RowVersion;
            ProductVersionAtRead = product?.RowVersion;
            StockAtRead = product?.Stock;
            // Cả hai phải đọc xong Pending trước khi service nào được sửa/lưu đơn.
            await gate.WaitForBothAsync();
            return order;
        }

        public Task<int> SaveChangeAsync()
        {
            SaveAttempts++;
            return inner.SaveChangeAsync();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync() => inner.BeginTransactionAsync();
        public Task<Cart?> GetCartForCheckoutAsync(int userId) => inner.GetCartForCheckoutAsync(userId);
        public Task<Order?> GetByIdForUserAsync(int id, int userId) => inner.GetByIdForUserAsync(id, userId);
        public Task<Order?> GetByIdForAdminAsync(int id) => inner.GetByIdForAdminAsync(id);
        public Task<Order?> GetOrderForAdminUpdateAsync(int orderId) => inner.GetOrderForAdminUpdateAsync(orderId);
        public Task<int> CountForUserAsync(int userId, OrderStatus? status) => inner.CountForUserAsync(userId, status);
        public Task<List<Order>> GetPageForUserAsync(int userId, int page, int size, OrderStatus? status)
            => inner.GetPageForUserAsync(userId, page, size, status);
        public Task<int> CountForAdminAsync(OrderStatus? status) => inner.CountForAdminAsync(status);
        public Task<List<Order>> GetPageForAdminAsync(int page, int size, OrderStatus? status)
            => inner.GetPageForAdminAsync(page, size, status);
        public void Add(Order order) => inner.Add(order);
        public void RemoveCartItems(IEnumerable<CartItem> items) => inner.RemoveCartItems(items);
    }
}
