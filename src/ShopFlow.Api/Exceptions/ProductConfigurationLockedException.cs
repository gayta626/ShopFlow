
namespace ShopFlow.Api.Exceptions;

public class ProductConfigurationLockedException : Exception
{
    public ProductConfigurationLockedException() : base("Sản phẩm đã có đơn hàng, không thể thay đổi cấu hình. Hãy tạo SKU mới")
    {

    }
}