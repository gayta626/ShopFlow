
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;
using ShopFlow.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ShopFlow.Api.Data;
using ShopFlow.Api.Dtos.Orders;
namespace ShopFlow.Tests;



public class CheckoutIntegrationTests
{

    [Fact]
    public async Task CheckoutAsync_WhenTwoCustomersBuyLastItem_OnlyOneSucceeds()
    {
        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only")
            .Options;

        await using var setupDb = new ShopFlowDbContext(options);
        await setupDb.Database.MigrateAsync();

        var suffix = Guid.NewGuid().ToString("N");
        string name = "Race-" + suffix;
        string normalizedName = name.ToUpperInvariant();
        var category = new Category
        {
            Name = name,
            NormalizedName = normalizedName
        };

        var product = new Product
        {
            Sku = "RACE-" + suffix,
            Name = "Keyboard race test",
            Brand = "Demo",
            Color = "Black",
            SwitchType = SwitchType.Linear,
            Connectivity = Connectivity.Wired,
            IsHotSwappable = true,
            Description = "Du lieu kiem thu checkout",
            Price = 100000m,
            Stock = 1,
            IsActive = true,
            Category = category
        };

        setupDb.Products.Add(product);
        string email1 = "customer-a-" + suffix + "@example.com";
        string normalizedEmail1 = email1.ToUpperInvariant();
        var userA = new User
        {
            Email = email1,
            NormalizedEmail = normalizedEmail1,
            FullName = "Cutomer A",
            Role = UserRole.Customer,
            PasswordHash = "unused-in-checkout-test"
        };
        string email2 = "customer-b-" + suffix + "@example.com";
        string normalizedEmail2 = email2.ToUpperInvariant();
        var userB = new User
        {
            Email = email2,
            NormalizedEmail = normalizedEmail2,
            FullName = "Cutomer B",
            Role = UserRole.Customer,
            PasswordHash = "unused-in-checkout-test"
        };

        var cartA = new Cart
        {
            User = userA,
            Revision = 0,
        };
        userA.Cart = cartA;
        var cartB = new Cart
        {
            User = userB,
            Revision = 0,
        };
        userB.Cart = cartB;

        cartA.Items.Add(new CartItem
        {
            Cart = cartA,
            Product = product,
            Quantity = 1
        });
        cartB.Items.Add(new CartItem
        {
            Cart = cartB,
            Product = product,
            Quantity = 1
        });

        setupDb.Users.AddRange(userA, userB);
        try
        {
            await setupDb.SaveChangesAsync();

            await using var firstDb = new ShopFlowDbContext(options);
            await using var secondDb = new ShopFlowDbContext(options);
            var gate = new CheckoutGate();

            var firstRepository = new CoordinateOrderRepository(new OrderRepository(firstDb), gate);
            var secondRepository = new CoordinateOrderRepository(new OrderRepository(secondDb), gate);

            var firstService = new OrderService(firstRepository);
            var secondService = new OrderService(secondRepository);

            OrderResponse? firstResponse = null;
            OrderResponse? secondResponse = null;

            // Khởi động cả hai lượt trước khi đợi để lượt đầu không chờ một mình tại cổng.
            var firstAttempt = Record.ExceptionAsync(async () =>
            {
                firstResponse = await firstService.CheckoutAsync(userA.Id, new CheckoutRequest
                {
                    ShippingAddress = "123 Nguyễn Trãi, Quận 5, TP.HCM"
                });
            });

            var secondAttempt = Record.ExceptionAsync(async () =>
            {
                secondResponse = await secondService.CheckoutAsync(userB.Id, new CheckoutRequest
                {
                    ShippingAddress = "123 Nguyễn Trãi, Quận 5, TP.HCM"
                });
            });

            var errors = await Task.WhenAll(firstAttempt, secondAttempt);

            Assert.Equal(2, gate.Arrivals);
            Assert.Equal(1L, firstRepository.StockAtRead);
            Assert.Equal(1L, secondRepository.StockAtRead);
            Assert.Equal(firstRepository.VersionAtRead, secondRepository.VersionAtRead);
            Assert.Equal(1, firstRepository.SaveAttempts);
            Assert.Equal(1, secondRepository.SaveAttempts);
            Assert.Single(errors, error => error is null);
            Assert.IsType<OrderConcurrencyException>(Assert.Single(errors, error => error is not null));

            var firstSucceeded = errors[0] is null;
            var winningResponse = firstSucceeded ? firstResponse : secondResponse;
            Assert.NotNull(winningResponse);
            Assert.Null(firstSucceeded ? secondResponse : firstResponse);
            var winnerUserId = firstSucceeded ? userA.Id : userB.Id;
            var loserUserId = firstSucceeded ? userB.Id : userA.Id;

            // Context mới đọc dữ liệu đã commit, không dùng entity đang giữ trong change tracker cũ.
            await using var verificationDb = new ShopFlowDbContext(options);
            var savedProduct = await verificationDb.Products.AsNoTracking().SingleAsync(p => p.Id == product.Id);
            Assert.Equal(0L, savedProduct.Stock);
            var order = Assert.Single(await verificationDb.Orders.AsNoTracking().Include(o => o.Items)
                .Where(o => o.UserId == userA.Id || o.UserId == userB.Id).ToListAsync());
            Assert.Equal(winnerUserId, order.UserId);
            Assert.Equal(winningResponse.Id, order.Id);
            Assert.Equal(OrderStatus.Pending, order.Status);
            Assert.Equal(100000m, order.TotalAmount);
            Assert.Equal("123 Nguyễn Trãi, Quận 5, TP.HCM", order.ShippingAddress);
            var orderItem = Assert.Single(order.Items);
            Assert.Equal(product.Id, orderItem.ProductId);
            Assert.Equal(product.Sku, orderItem.Sku);
            Assert.Equal(product.Name, orderItem.ProductName);
            Assert.Equal(100000m, orderItem.UnitPrice);
            Assert.Equal(1, orderItem.Quantity);
            Assert.Equal(order.TotalAmount, winningResponse.TotalAmount);
            Assert.Equal("Pending", winningResponse.Status);
            var responseItem = Assert.Single(winningResponse.Items);
            Assert.Equal(product.Id, responseItem.ProductId);
            Assert.Equal(product.Sku, responseItem.Sku);
            Assert.Equal(product.Name, responseItem.ProductName);
            Assert.Equal(100000m, responseItem.UnitPrice);
            Assert.Equal(1, responseItem.Quantity);
            Assert.Equal(100000m, responseItem.LineTotal);

            var carts = await verificationDb.Carts.AsNoTracking().Include(c => c.Items)
                .Where(c => c.UserId == userA.Id || c.UserId == userB.Id).ToListAsync();
            var winnerCart = Assert.Single(carts, c => c.UserId == winnerUserId);
            var loserCart = Assert.Single(carts, c => c.UserId == loserUserId);
            Assert.Empty(winnerCart.Items);
            Assert.Equal(1L, winnerCart.Revision);
            Assert.Equal(0L, loserCart.Revision);
            var remainingItem = Assert.Single(loserCart.Items);
            Assert.Equal(product.Id, remainingItem.ProductID);
            Assert.Equal(1, remainingItem.Quantity);
        }
        finally
        {
            // Chỉ dọn dữ liệu của lần chạy này, kể cả khi chuẩn bị hoặc Assert thất bại.
            await using var cleanupDb = new ShopFlowDbContext(options);
            var users = await cleanupDb.Users
                .Where(u => u.NormalizedEmail == normalizedEmail1 || u.NormalizedEmail == normalizedEmail2)
                .ToListAsync();
            var userIds = users.Select(u => u.Id).ToArray();
            var orders = await cleanupDb.Orders.Include(o => o.Items)
                .Where(o => userIds.Contains(o.UserId)).ToListAsync();
            var carts = await cleanupDb.Carts.Include(c => c.Items)
                .Where(c => userIds.Contains(c.UserId)).ToListAsync();
            cleanupDb.OrderItems.RemoveRange(orders.SelectMany(o => o.Items));
            cleanupDb.Orders.RemoveRange(orders);
            cleanupDb.CartItems.RemoveRange(carts.SelectMany(c => c.Items));
            cleanupDb.Carts.RemoveRange(carts);
            cleanupDb.Products.RemoveRange(await cleanupDb.Products.Where(p => p.Sku == product.Sku).ToListAsync());
            cleanupDb.Categories.RemoveRange(await cleanupDb.Categories
                .Where(c => c.NormalizedName == normalizedName).ToListAsync());
            cleanupDb.Users.RemoveRange(users);
            await cleanupDb.SaveChangesAsync();
        }

    }

    private sealed class CheckoutGate
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

    private sealed class CoordinateOrderRepository : IOrderRepository
    {
        private readonly OrderRepository _inner;
        private readonly CheckoutGate _gate;
        public long? StockAtRead { get; private set; }
        public uint? VersionAtRead { get; private set; }
        public int SaveAttempts { get; private set; }

        public CoordinateOrderRepository(OrderRepository inner, CheckoutGate gate)
        {
            _inner = inner;
            _gate = gate;
        }

        public async Task<Cart?> GetCartForCheckoutAsync(int userId)
        {
            var cart = await _inner.GetCartForCheckoutAsync(userId);
            var item = cart?.Items.SingleOrDefault();
            StockAtRead = item?.Product.Stock;
            VersionAtRead = item?.Product.RowVersion;
            // Chờ cả hai lượt đọc xong giỏ và Product trước khi cho service tiếp tục.
            await _gate.WaitForBothAsync();
            return cart;
        }

        public Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return _inner.BeginTransactionAsync();
        }

        public Task<Order?> GetByIdForUserAsync(int id, int userId)
        {
            return _inner.GetByIdForUserAsync(id, userId);
        }

        public Task<Order?> GetOrderForUpdateAsync(int orderId, int userId)
        {
            return _inner.GetOrderForUpdateAsync(orderId, userId);
        }

        public Task<Order?> GetOrderForAdminUpdateAsync(int orderId)
        {
            return _inner.GetOrderForAdminUpdateAsync(orderId);
        }

        public Task<int> CountForUserAsync(int userId, OrderStatus? status)
        {
            return _inner.CountForUserAsync(userId, status);
        }

        public Task<List<Order>> GetPageForUserAsync(int userId, int page, int size, OrderStatus? status)
        {
            return _inner.GetPageForUserAsync(userId, page, size, status);
        }

        public void Add(Order order)
        {
            _inner.Add(order);
        }

        public void RemoveCartItems(IEnumerable<CartItem> items)
        {
            _inner.RemoveCartItems(items);
        }

        public Task<int> SaveChangeAsync()
        {
            SaveAttempts++;
            return _inner.SaveChangeAsync();
        }

    }
}
