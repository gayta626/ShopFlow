namespace ShopFlow.Api.Exceptions;

// Yêu cầu giao diện hỏi xác nhận trước khi đổi tên danh mục có sản phẩm.
public class CategoryRenameConfirmationRequiredException : Exception
{
    public CategoryRenameConfirmationRequiredException() : base("Danh mục đã có sản phẩm. Đổi tên sẽ thay đổi tên layout hiển thị của tất cả sản phẩm thuộc danh mục. Bạn có chắc muốn tiếp tục?")
    {
    }
}
