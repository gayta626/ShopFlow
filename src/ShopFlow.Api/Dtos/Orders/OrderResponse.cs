

using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Dtos.Orders;

public class OrderResponse
{
    public int Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public string ShippingAddress { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<OrderItemResponse> Items { get; set; } = new List<OrderItemResponse>();
}