

using System.ComponentModel.DataAnnotations;

namespace ShopFlow.Api.Dtos.Carts;

public class AddCartItemRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 99)]
    public int Quantity { get; set; }
}
