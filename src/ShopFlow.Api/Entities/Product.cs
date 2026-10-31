

namespace ShopFlow.Api.Entities;

public class Product
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public SwitchType SwitchType { get; set; }
    public Connectivity Connectivity { get; set; }
    public string Color { get; set; } = string.Empty;
    public bool? IsHotSwappable { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Stock { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public uint RowVersion { get; set; }

    // TODO: Add the CategoryId foreign key and Category navigation property.
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

}
