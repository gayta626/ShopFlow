

using ShopFlow.Api.Entities;

namespace ShopFlow.Api.Dtos.Products;

public class AdminProductResponse
{
    public int Id { get; set; }
    public int CategoryId { get; set; }

    // Lấy từ Category.Name, không phải một cột riêng trong Product.
    public string Layout { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public SwitchType SwitchType { get; set; }

    // Service tách flags trong entity thành từng loại kết nối khi map response.
    public List<Connectivity> Connectivity { get; set; } = new();

    public string Color { get; set; } = string.Empty;
    public bool? IsHotSwappable { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public long Stock { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Admin gửi lại phiên bản này khi cập nhật để phát hiện dữ liệu đã thay đổi.
    public uint RowVersion { get; set; }
}
