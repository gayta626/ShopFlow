

namespace ShopFlow.Api.Entities;

public class User
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public UserRole Role { get; set; }


    public Cart? Cart { get; set; }


    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}