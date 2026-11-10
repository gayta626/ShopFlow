using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ShopFlow.Api.Dtos.Auth;
using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Services;

namespace ShopFlow.Tests;

public class AuthServiceTests
{
    [Fact] // Đánh dấu để xUnit tìm và chạy bài test này.
    public async Task ValidateCredentialsAsync_WhenRehashNeeded_UpdatesHashAndSaves()
    {
        // ARRANGE: chuẩn bị user trong bộ nhớ, không ghi database thật.
        var user = new User
        {
            Id = 4,
            Email = "learner@example.com",
            NormalizedEmail = "learner@example.com",
            PasswordHash = "old-hash" // Hash minh họa, chỉ dùng với fake hasher.
        };
        var request = new LoginRequest // Dữ liệu đăng nhập giả của bài test.
        {
            Email = "learner@example.com",
            Password = "Test-password-123!"
        };
        var repository = new FakeUserRepository(user); // Trả user đã chuẩn bị và ghi nhận lời gọi lưu.
        var hasher = new FakePasswordHasher(); // Chủ động tạo tình huống cần nâng cấp hash.
        var configuration = new ConfigurationBuilder().Build(); // Cấu hình rỗng vì bài này không cấp token.
        var tokenService = new JwtTokenService(configuration); // Dependency bắt buộc của constructor.
        var service = new AuthService(repository, hasher, tokenService); // Service THẬT được kiểm tra.

        // ACT: gọi phương thức cần kiểm tra đúng một lần.
        var result = await service.ValidateCredentialsAsync(request);

        // ASSERT: bất kỳ điều kiện nào sai đều khiến bài test thất bại.
        Assert.Same(user, result); // Trả đúng user để tiếp tục đăng nhập.
        Assert.Equal("old-hash", hasher.VerifiedHash); // Kiểm tra bằng hash cũ trước khi thay đổi.
        Assert.Equal(request.Password, hasher.VerifiedPassword); // Xác minh đúng mật khẩu từ request.
        Assert.Same(user, hasher.HashedUser); // Tạo hash cho đúng user.
        Assert.Equal(request.Password, hasher.HashedPassword); // Hash mật khẩu gốc, không hash chuỗi old-hash.
        Assert.Equal(1, hasher.HashCallCount); // Chỉ tạo hash mới một lần.
        Assert.Equal("new-hash", user.PasswordHash); // Gán hash mới vào user.
        Assert.Equal(1, repository.SaveCallCount); // Yêu cầu lưu đúng một lần.
        Assert.Equal("new-hash", repository.HashAtSave); // Hash phải được gán TRƯỚC khi gọi lưu.
    }

    [Theory] // Chạy cùng một bài test với từng bộ dữ liệu InlineData bên dưới.
    [InlineData(PasswordVerificationResult.Failed, false)] // Mật khẩu sai: trả null.
    [InlineData(PasswordVerificationResult.Success, true)] // Mật khẩu đúng, hash còn phù hợp: trả user.
    public async Task ValidateCredentialsAsync_WhenNoRehashRequired_DoesNotChangeHashOrSave(
        PasswordVerificationResult verificationResult, bool shouldAuthenticate)
    {
        // ARRANGE: mỗi lần chạy có user và các fake riêng, không chia sẻ bộ đếm.
        var user = new User
        {
            Id = 4,
            Email = "learner@example.com",
            NormalizedEmail = "learner@example.com",
            PasswordHash = "existing-hash"
        };
        var request = new LoginRequest
        {
            Email = "learner@example.com",
            Password = "Test-password-123!"
        };
        var repository = new FakeUserRepository(user);
        var hasher = new FakePasswordHasher { VerificationResult = verificationResult }; // Chọn kết quả xác minh theo dữ liệu test.
        var tokenService = new JwtTokenService(new ConfigurationBuilder().Build());
        var service = new AuthService(repository, hasher, tokenService);

        // ACT: service thật phải xử lý đúng cả Failed và Success.
        var result = await service.ValidateCredentialsAsync(request);

        // ASSERT: kiểm tra kết quả đăng nhập và bảo đảm không ghi dữ liệu ngoài ý muốn.
        if (shouldAuthenticate)
            Assert.Same(user, result); // Success phải trả đúng user.
        else
            Assert.Null(result); // Failed không được cho phép tiếp tục đăng nhập.

        Assert.Equal("existing-hash", hasher.VerifiedHash); // Đã xác minh bằng hash đang lưu.
        Assert.Equal(request.Password, hasher.VerifiedPassword); // Đã dùng mật khẩu từ request.
        Assert.Equal("existing-hash", user.PasswordHash); // Giữ nguyên hash ở cả hai trường hợp.
        Assert.Equal(0, hasher.HashCallCount); // Không tạo hash mới nếu không cần nâng cấp.
        Assert.Equal(0, repository.SaveCallCount); // Không yêu cầu lưu khi không thay đổi dữ liệu.
        Assert.Null(repository.HashAtSave); // Fake chưa ghi nhận lần lưu nào.
    }

    // Class lồng trong test, chỉ dùng cho bài mẫu; không thay repository của ứng dụng.
    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user; // Giữ user do phần Arrange chuẩn bị.
        public int SaveCallCount { get; private set; } // Test đọc bộ đếm, chỉ fake được thay đổi.
        public string? HashAtSave { get; private set; } // Hash được chụp ở thời điểm lưu.

        public FakeUserRepository(User user)
        {
            _user = user; // Dùng user trong bộ nhớ thay cho database.
        }

        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail)
        {
            return Task.FromResult<User?>( // Tạo Task đã hoàn thành để khớp interface async.
                normalizedEmail == _user.NormalizedEmail ? _user : null); // Chỉ trả user khi email khớp.
        }

        public void Add(User user)
        {
            throw new NotSupportedException("Test đăng nhập không được thêm user mới."); // Báo lỗi nếu gọi nhầm Add.
        }

        public Task<int> SaveChangeAsync()
        {
            SaveCallCount++; // Ghi nhận yêu cầu lưu.
            HashAtSave = _user.PasswordHash; // Chụp giá trị tại thời điểm gọi lưu.
            return Task.FromResult(1); // Giả lập lưu một entity, không kết nối PostgreSQL.
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher<User>
    {
        public PasswordVerificationResult VerificationResult { get; init; }
            = PasswordVerificationResult.SuccessRehashNeeded; // Cho phép mỗi test chọn kết quả; bài mẫu đầu giữ mặc định cũ.
        public string? VerifiedHash { get; private set; } // Hash được truyền vào để xác minh.
        public string? VerifiedPassword { get; private set; } // Mật khẩu được truyền vào để xác minh.
        public User? HashedUser { get; private set; } // User nhận hash mới.
        public string? HashedPassword { get; private set; } // Mật khẩu được dùng tạo hash mới.
        public int HashCallCount { get; private set; } // Số lần gọi tạo hash.

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            VerifiedHash = hashedPassword; // Ghi nhận đầu vào để Assert kiểm tra.
            VerifiedPassword = providedPassword;
            return VerificationResult; // Trả kết quả mà phần Arrange đã chọn.
        }

        public string HashPassword(User user, string password)
        {
            HashCallCount++; // Đếm lần service gọi hàm này.
            HashedUser = user; // Ghi nhận user để kiểm tra service truyền đúng đối tượng.
            HashedPassword = password; // Ghi nhận mật khẩu để phát hiện hash nhầm dữ liệu.
            return "new-hash"; // Kết quả giả cố định; bài này kiểm tra service, không kiểm tra thuật toán hash.
        }
    }
}
