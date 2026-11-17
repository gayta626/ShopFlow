

namespace ShopFlow.Api.Dtos.Orders;

public class OrderListResponse
{
    public List<OrderSummaryResponse> Items { get; set; } = new List<OrderSummaryResponse>();

    public int Page { get; set; }

    public int Size { get; set; }

    public int TotalCount { get; set; }
}