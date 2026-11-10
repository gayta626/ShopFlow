

namespace ShopFlow.Api.Dtos.Products;

public class ProductListResponse
{
    public List<ProductsResponse> Items { get; set; } = new();

    public int Page { get; set; }

    public int Size { get; set; }

    public int TotalCount { get; set; }
}
