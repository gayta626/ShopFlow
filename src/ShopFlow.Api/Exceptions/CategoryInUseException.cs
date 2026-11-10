

namespace ShopFlow.Api.Exceptions;

public class CategoryInUseException : Exception
{
    public CategoryInUseException() : base("Khong the xoa danh muc dang co san pham")
    {

    }
}