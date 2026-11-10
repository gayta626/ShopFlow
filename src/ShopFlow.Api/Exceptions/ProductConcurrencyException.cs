namespace ShopFlow.Api.Exceptions;

public class ProductConcurrencyException : Exception
{
    public ProductConcurrencyException(Exception innerException)
        : base("Sản phẩm đã thay đổi. Vui lòng tải lại sản phẩm và thử lại.", innerException)
    {
    }
}
