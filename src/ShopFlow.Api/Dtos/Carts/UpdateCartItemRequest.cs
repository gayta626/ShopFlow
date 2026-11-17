
using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Carts;

public class UpdateCartItemRequest
{
    [Range(1, 99)]
    public int Quantity { get; set; }
}