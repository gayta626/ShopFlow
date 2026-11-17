namespace ShopFlow.Api.Exceptions;

public class CartConcurrencyException : Exception
{
    public CartConcurrencyException(Exception innerException)
        : base("Giỏ hàng đã thay đổi. Vui lòng tải lại và thử lại.", innerException)
    {
    }
}
