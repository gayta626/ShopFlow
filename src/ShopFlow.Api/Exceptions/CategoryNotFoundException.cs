

namespace ShopFlow.Api.Exceptions;

public class CategoryNotFoundException : Exception
{
    public CategoryNotFoundException() : base("Danh muc khong ton tai hoac da bi xoa")
    {

    }
}