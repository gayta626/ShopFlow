
using System.Collections.ObjectModel;

namespace ShopFlow.Api.Entities;

public class Cart
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public User User { get; set; } = null!;

    public long Revision { get; set; }

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();

}