
namespace ShopFlow.Api.Entities;

public class Order
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string ShippingAddress { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? PaidAt { get; set; }

    public DateTimeOffset? ShippedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    public uint RowVersion { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
