

namespace ShopFlow.Api.Dtos.Orders;

public class OrderSummaryResponse
{
    public int Id { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}