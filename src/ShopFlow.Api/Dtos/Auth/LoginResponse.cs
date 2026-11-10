
namespace ShopFlow.Api.Dtos.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAt { get; set; }

    public LoginUserResponse User { get; set; } = null!;
}
