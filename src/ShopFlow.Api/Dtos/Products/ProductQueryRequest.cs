using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Dtos.Products;

public class ProductQueryRequest
{
    // Nhận ?q=... trên URL; không truyền thì không lọc từ khóa.
    public string? Q { get; set; }

    public int? CategoryId { get; set; }

    public SwitchType? SwitchType { get; set; }

    // Khách chọn một kết nối cần hỗ trợ, không phải danh sách kết nối của sản phẩm.
    public Connectivity? Connectivity { get; set; }

    // Mỗi đầu khoảng giá là tùy chọn; hai biên đều được tính vào kết quả.
    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public string Sort { get; set; } = "newest";

    public int Page { get; set; } = 1;

    public int Size { get; set; } = 20;
}
