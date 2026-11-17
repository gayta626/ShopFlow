namespace ShopFlow.Api.Exceptions;

public class OrderConcurrencyException : Exception
{
    public OrderConcurrencyException(Exception innerException)
        : base("Giỏ hàng hoặc sản phẩm đã thay đổi. Vui lòng tải lại giỏ hàng.", innerException)
    {
    }
}
