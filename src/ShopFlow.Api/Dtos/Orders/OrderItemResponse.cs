

namespace ShopFlow.Api.Dtos.Orders;

public class OrderItemResponse
{
    public int ProductId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }
    public int Quantity { get; set; }
}