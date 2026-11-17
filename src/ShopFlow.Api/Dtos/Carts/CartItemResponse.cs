

namespace ShopFlow.Api.Dtos.Carts;

public class CartItemResponse
{
    // Sản phẩm được chọn trong dòng giỏ này.
    public int ProductId { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    // Số lượng khách đã thêm vào giỏ.
    public int Quantity { get; set; }

    // Giá và tồn kho hiện tại từ Product, chưa giữ giá hoặc giữ hàng.
    public decimal UnitPrice { get; set; }
    public long Stock { get; set; }


    public bool IsAvailable { get; set; }

    // Service tính tiền tạm tính của dòng: UnitPrice * Quantity.
    public decimal LineTotal { get; set; }
}
