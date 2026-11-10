namespace ShopFlow.Api.Exceptions;

public class CategoryConcurrencyException : Exception
{
    public CategoryConcurrencyException(Exception innerException)
        : base("Dữ liệu thay đổi đồng thời. Vui lòng tải lại danh mục và thử lại.", innerException)
    {
    }
}
