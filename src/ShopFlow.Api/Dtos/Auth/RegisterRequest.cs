
using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Auth;

public class RegisterRequest
{

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please check email format")]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FullName { get; set; } = string.Empty;
}