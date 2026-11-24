

namespace ShopFlow.Api.Dtos.Products;

public class ProductImageResponse
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
