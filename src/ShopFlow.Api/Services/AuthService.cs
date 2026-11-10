

using ShopFlow.Api.Entities;
using ShopFlow.Api.Repositories;
using ShopFlow.Api.Dtos.Auth;
using Microsoft.AspNetCore.Identity;
using ShopFlow.Api.Exceptions;


namespace ShopFlow.Api.Services;

public class AuthService
{
    private readonly IUserRepository _userRepository;

    private readonly IPasswordHasher<User> _passwordHasher;

    private readonly JwtTokenService _jwtTokenService;

    public AuthService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher, JwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<User?> ValidateCredentialsAsync(LoginRequest request)
    {
        string normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail);

        if (user == null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded) // Mật khẩu đúng nhưng hash cần nâng cấp.
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password); // Tạo hash mới từ mật khẩu vừa nhập theo cấu hình hiện tại.
            await _userRepository.SaveChangeAsync(); // EF đang theo dõi user nên lưu được PasswordHash mới trước khi cấp JWT.
        }

        return user;
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        string email = request.Email.Trim();
        string normalizedEmail = email.ToLowerInvariant();
        string fullName = request.FullName.Trim();

        if (string.IsNullOrWhiteSpace(email) || fullName.Length < 2 || fullName.Length > 100)
        {
            throw new ArgumentException("Sai dinh dang ");
        }

        var existingUser = await _userRepository.GetByNormalizedEmailAsync(normalizedEmail);

        if (existingUser != null)
        {
            throw new EmailAlreadyExistsException();
        }
        User user = new User();
        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.FullName = fullName;
        user.Role = UserRole.Customer;
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

        Cart cart = new Cart();
        cart.User = user;
        user.Cart = cart;

        _userRepository.Add(user);
        await _userRepository.SaveChangeAsync();

        return new RegisterResponse
        {
            Email = user.Email,
            FullName = user.FullName
        };
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await ValidateCredentialsAsync(request);
        if (user == null) return null;

        var token = _jwtTokenService.CreateToken(user);
        var response = new LoginResponse
        {
            AccessToken = token.AccessToken,
            ExpiresAt = token.ExpiresAt,
            TokenType = "Bearer",
            User = new LoginUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role.ToString()
            }
        };
        return response;
    }


}
