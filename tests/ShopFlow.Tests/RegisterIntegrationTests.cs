
using Microsoft.EntityFrameworkCore; // Cung cấp UseNpgsql và MigrateAsync.
using ShopFlow.Api.Data; // Cho phép dùng ShopFlowDbContext.

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShopFlow.Api.Dtos.Auth;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;
using Npgsql; // Đọc mã lỗi và thông báo thật từ PostgreSQL.
using ShopFlow.Api.Exceptions;

namespace ShopFlow.Tests;

public class RegisterIntegrationTests
{
    [Fact]
    public async Task RegisterAsync_WithConcurrentSameEmail_CreatesOnlyOneUserAndCart()
    {
        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only")
            .Options;
        await using var setupDb = new ShopFlowDbContext(options);
        await setupDb.Database.MigrateAsync();
        var email = $"race-{Guid.NewGuid():N}@example.com";

        // Mỗi lần đăng ký có DbContext riêng: DbContext không được dùng đồng thời giữa hai tác vụ.
        await using var firstDb = new ShopFlowDbContext(options);
        await using var secondDb = new ShopFlowDbContext(options);
        var gate = new EmailLookupGate(); // Chặn tại cuối bước tìm email cho tới khi cả hai đã đọc DB.
        var firstRepository = new CoordinatedUserRepository(new UserRepository(firstDb), gate);
        var secondRepository = new CoordinatedUserRepository(new UserRepository(secondDb), gate);
        var tokenService = new JwtTokenService(new ConfigurationBuilder().Build());
        var firstService = new AuthService(firstRepository, new PasswordHasher<User>(), tokenService);
        var secondService = new AuthService(secondRepository, new PasswordHasher<User>(), tokenService);

        try
        {
            // ACT: hai lời gọi service thật cùng email; không dùng delay để đoán thời điểm tranh chấp.
            var firstAttempt = Record.ExceptionAsync(async () =>
            {
                await firstService.RegisterAsync(new RegisterRequest
                {
                    Email = email, FullName = "First Customer", Password = "Test-password-123!"
                });
            });
            var secondAttempt = Record.ExceptionAsync(async () =>
            {
                await secondService.RegisterAsync(new RegisterRequest
                {
                    Email = email, FullName = "Second Customer", Password = "Test-password-123!"
                });
            });
            var errors = await Task.WhenAll(firstAttempt, secondAttempt); // Đợi cả hai kết thúc trước khi đọc/dọn DB.

            // ASSERT: cả hai đều thấy email chưa tồn tại và thực sự thử lưu.
            Assert.Equal(2, gate.Arrivals);
            Assert.Null(firstRepository.LookupResult);
            Assert.Null(secondRepository.LookupResult);
            Assert.Equal(1, firstRepository.SaveAttempts);
            Assert.Equal(1, secondRepository.SaveAttempts);
            Assert.Single(errors, error => error is null); // Chính xác một đăng ký thành công.
            Assert.IsType<EmailAlreadyExistsException>(Assert.Single(errors, error => error is not null));

            await using var verificationDb = new ShopFlowDbContext(options);
            var savedUser = Assert.Single(await verificationDb.Users.AsNoTracking()
                .Where(user => user.NormalizedEmail == email).ToListAsync());
            Assert.Single(await verificationDb.Carts.AsNoTracking()
                .Where(cart => cart.UserId == savedUser.Id).ToListAsync());
        }
        finally
        {
            // Chỉ dọn dữ liệu có email riêng của lần chạy, không xóa dữ liệu các test khác.
            await using var cleanupDb = new ShopFlowDbContext(options);
            var users = await cleanupDb.Users.Where(user => user.NormalizedEmail == email).ToListAsync();
            var userIds = users.Select(user => user.Id).ToArray();
            var carts = await cleanupDb.Carts.Where(cart => userIds.Contains(cart.UserId)).ToListAsync();
            cleanupDb.Carts.RemoveRange(carts);
            cleanupDb.Users.RemoveRange(users);
            await cleanupDb.SaveChangesAsync();
        }
    }

    // Cổng đồng bộ bất đồng bộ: lần gọi đầu đợi lần gọi thứ hai hoàn tất truy vấn email.
    private sealed class EmailLookupGate
    {
        private readonly TaskCompletionSource<bool> _bothReady = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _arrivals;
        public int Arrivals => Volatile.Read(ref _arrivals);

        public async Task WaitForBothAsync()
        {
            if (Interlocked.Increment(ref _arrivals) == 2)
                _bothReady.TrySetResult(true);
            await _bothReady.Task.WaitAsync(TimeSpan.FromSeconds(20)); // Báo lỗi thay vì treo nếu một lời gọi không đến được đây.
        }
    }

    // Wrapper chỉ điều khiển thời điểm; mọi truy vấn và lưu vẫn dùng UserRepository/PostgreSQL thật.
    private sealed class CoordinatedUserRepository(UserRepository inner, EmailLookupGate gate) : IUserRepository
    {
        public User? LookupResult { get; private set; }
        public int SaveAttempts { get; private set; }

        public async Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
        {
            LookupResult = await inner.GetByNormalizedEmailAsync(normalizedEmail);
            await gate.WaitForBothAsync(); // Không cho service nào lưu trước khi cả hai truy vấn hoàn tất.
            return LookupResult;
        }

        public void Add(User user) => inner.Add(user);

        public async Task<int> SaveChangeAsync()
        {
            SaveAttempts++;
            return await inner.SaveChangeAsync(); // Repository thật chuyển unique violation thành lỗi email trùng.
        }
    }

    [Fact]
    public async Task RegisterAsync_WhenCartInsertFails_RollsBackUserAndCart()
    {
        // ARRANGE: chỉ dùng database test; không tạo transaction bao ngoài để che lỗi của service.
        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql("Host=localhost;Port=5434;Database=shopflow_test;Username=shopflow_test;Password=local_test_only")
            .Options;
        await using var db = new ShopFlowDbContext(options);
        await db.Database.MigrateAsync();

        var suffix = Guid.NewGuid().ToString("N"); // Tên riêng cho mỗi lần chạy, chỉ gồm ký tự hex.
        var email = $"rollback-{suffix}@example.com";
        var functionName = $"test_cart_failure_{suffix}";
        var triggerName = $"test_cart_trigger_{suffix}";
        const string errorMarker = "SHOPFLOW_TEST_CART_FAILURE";

        try
        {
            // AFTER INSERT: Cart đã được insert trong transaction rồi mới chủ động gây lỗi.
            // Chỉ chặn Cart của email test. EXISTS còn chứng minh User đã tồn tại bên trong transaction.
            // SQL chỉ nội suy tên/email sinh từ Guid ở trên, không nhận dữ liệu đầu vào của người dùng.
            var setupSql = $$"""
                CREATE FUNCTION public.{{functionName}}() RETURNS trigger
                LANGUAGE plpgsql AS $test$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM public."Users"
                        WHERE "Id" = NEW."UserId" AND "NormalizedEmail" = '{{email}}'
                    ) THEN
                        RAISE EXCEPTION '{{errorMarker}}:%', NEW."UserId"
                            USING ERRCODE = 'P0001';
                    END IF;
                    RETURN NEW;
                END;
                $test$;
                CREATE TRIGGER {{triggerName}}
                AFTER INSERT ON public."Carts"
                FOR EACH ROW EXECUTE FUNCTION public.{{functionName}}();
                """;
            await db.Database.ExecuteSqlRawAsync(setupSql);

            var request = new RegisterRequest
            {
                Email = email,
                FullName = "Rollback Test User",
                Password = "Test-password-123!"
            };
            var service = new AuthService(new UserRepository(db), new PasswordHasher<User>(),
                new JwtTokenService(new ConfigurationBuilder().Build()));

            // ACT: phải thất bại đúng ở bước lưu Cart, không phải ở validation hoặc kết nối.
            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => service.RegisterAsync(request));
            var postgresError = Assert.IsType<PostgresException>(exception.InnerException);
            Assert.Equal("P0001", postgresError.SqlState);
            Assert.StartsWith(errorMarker + ":", postgresError.MessageText);
            var insertedUserId = int.Parse(postgresError.MessageText[(errorMarker.Length + 1)..]);
            Assert.True(insertedUserId > 0); // ID đã được PostgreSQL cấp trước khi phát sinh lỗi.

            // ASSERT: context mới đọc trạng thái thực sau rollback, không đọc change tracker cũ.
            await using var verificationDb = new ShopFlowDbContext(options);
            Assert.False(await verificationDb.Users.AnyAsync(user => user.NormalizedEmail == email));
            Assert.False(await verificationDb.Users.AnyAsync(user => user.Id == insertedUserId));
            Assert.False(await verificationDb.Carts.AnyAsync(cart => cart.UserId == insertedUserId));
        }
        finally
        {
            // Gỡ cơ chế gây lỗi kể cả khi Assert thất bại; dùng context mới sau lỗi lưu.
            await using var cleanupDb = new ShopFlowDbContext(options);
            // Tên đối tượng SQL không thể truyền bằng parameter; cả hai tên chỉ gồm prefix cố định + Guid hex.
#pragma warning disable EF1002
            await cleanupDb.Database.ExecuteSqlRawAsync(
                $"DROP TRIGGER IF EXISTS {triggerName} ON public.\"Carts\"; DROP FUNCTION IF EXISTS public.{functionName}();");
#pragma warning restore EF1002

            // Nếu rollback bị lỗi, dọn đúng dữ liệu của bài này sau khi các Assert đã phát hiện lỗi.
            var users = await cleanupDb.Users.Where(user => user.NormalizedEmail == email).ToListAsync();
            var userIds = users.Select(user => user.Id).ToArray();
            var carts = await cleanupDb.Carts.Where(cart => userIds.Contains(cart.UserId)).ToListAsync();
            cleanupDb.Carts.RemoveRange(carts);
            cleanupDb.Users.RemoveRange(users);
            await cleanupDb.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreateUserAndCart()
    {
        // ARRANGE: dùng riêng PostgreSQL test ở cổng 5434, không đọc cấu hình database ứng dụng.
        var connectionString =
           "Host=localhost;Port=5434;Database=shopflow_test;" +
           "Username=shopflow_test;Password=local_test_only";

        var options = new DbContextOptionsBuilder<ShopFlowDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var db = new ShopFlowDbContext(options);

        await db.Database.MigrateAsync(); // Tạo schema từ migration hiện có nếu database test chưa được cập nhật.

        var email = $"register-{Guid.NewGuid():N}@example.com"; // Email riêng để chạy lại không bị trùng dữ liệu.
        var request = new RegisterRequest
        {
            Email = email,
            FullName = "Integration Test User",
            Password = "Test-password-123!"
        };
        var hasher = new PasswordHasher<User>(); // Hash thật, không dùng fake như unit test.
        var repository = new UserRepository(db); // Repository thật ghi vào PostgreSQL test.
        var tokenService = new JwtTokenService(new ConfigurationBuilder().Build()); // Register không cấp JWT nên không cần key.
        var service = new AuthService(repository, hasher, tokenService);

        try
        {
            // ACT: thực hiện nghiệp vụ đăng ký thật.
            var response = await service.RegisterAsync(request);

            // ASSERT: context mới đọc dữ liệu đã lưu, không dùng đối tượng mà context đăng ký đang giữ.
            await using var verificationDb = new ShopFlowDbContext(options);
            var users = await verificationDb.Users.AsNoTracking()
                .Where(user => user.NormalizedEmail == email).ToListAsync();
            var savedUser = Assert.Single(users); // Database phải có đúng một user theo email test.

            Assert.Equal(email, savedUser.Email);
            Assert.Equal(request.FullName, savedUser.FullName);
            Assert.Equal(UserRole.Customer, savedUser.Role);
            Assert.NotEqual(request.Password, savedUser.PasswordHash); // Không lưu mật khẩu dạng rõ.
            Assert.Equal(PasswordVerificationResult.Success,
                hasher.VerifyHashedPassword(savedUser, savedUser.PasswordHash, request.Password)); // Hash thật phải xác minh được mật khẩu.

            var carts = await verificationDb.Carts.AsNoTracking()
                .Where(cart => cart.UserId == savedUser.Id).ToListAsync();
            var savedCart = Assert.Single(carts); // Có đúng một Cart thuộc user vừa đăng ký.
            Assert.Equal(savedUser.Id, savedCart.UserId);
            Assert.Equal(0L, savedCart.Revision);
            Assert.Equal(email, response.Email); // Kiểm tra cả response của service.
            Assert.Equal(request.FullName, response.FullName);
        }
        finally // Dọn cả khi Assert thất bại; chỉ xóa dữ liệu có email riêng của lần chạy này.
        {
            await using var cleanupDb = new ShopFlowDbContext(options);
            var testUsers = await cleanupDb.Users.Where(user => user.NormalizedEmail == email).ToListAsync();
            var testUserIds = testUsers.Select(user => user.Id).ToArray();
            var testCarts = await cleanupDb.Carts.Where(cart => testUserIds.Contains(cart.UserId)).ToListAsync();
            cleanupDb.Carts.RemoveRange(testCarts); // Xóa Cart phụ thuộc trước User vì FK dùng Restrict.
            cleanupDb.Users.RemoveRange(testUsers);
            await cleanupDb.SaveChangesAsync(); // EF sắp thứ tự DELETE theo quan hệ khóa ngoại.
        }
    }


}

