

namespace ShopFlow.Api.Dtos.Products;

public class AdminProductListResponse
{
    public List<AdminProductResponse> Items { get; set; } = new();

    public int Page { get; set; }

    public int Size { get; set; }

    public int TotalCount { get; set; }
}
