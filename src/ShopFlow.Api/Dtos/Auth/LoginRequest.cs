

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please check email format")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please enter the password")]
    public string Password { get; set; } = string.Empty;
}
