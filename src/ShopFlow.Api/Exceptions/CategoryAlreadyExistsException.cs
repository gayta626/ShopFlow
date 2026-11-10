namespace ShopFlow.Api.Exceptions;

// Service/controller dùng lỗi này để xử lý tên danh mục trùng mà không phụ thuộc PostgreSQL.
public class CategoryAlreadyExistsException : Exception
{
    public CategoryAlreadyExistsException() : base("Tên danh mục đã được sử dụng.")
    {
    }
}
