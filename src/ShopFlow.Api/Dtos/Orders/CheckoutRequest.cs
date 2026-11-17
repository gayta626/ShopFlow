

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Orders;

public class CheckoutRequest
{
    [Required]
    [StringLength(500, MinimumLength = 10)]
    public string ShippingAddress { get; set; } = string.Empty;

}