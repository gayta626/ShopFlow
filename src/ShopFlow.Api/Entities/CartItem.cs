

namespace ShopFlow.Api.Entities;

public class CartItem
{
    public int Id { get; set; }

    public int CartId { get; set; }

    public Cart Cart { get; set; } = null!;

    public int ProductID { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}