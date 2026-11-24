

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Auth;

public class UpdateProfileRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FullName { get; set; } = string.Empty;
}
