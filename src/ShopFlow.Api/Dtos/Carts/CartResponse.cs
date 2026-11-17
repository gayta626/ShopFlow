

using System.ComponentModel.DataAnnotations;
using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Dtos.Carts;

public class CartResponse
{
    [Range(1, int.MaxValue)]
    public List<CartItemResponse> Items { get; set; } = null!;

    [Range(1, 99)]
    public decimal TotalAmount { get; set; }
}