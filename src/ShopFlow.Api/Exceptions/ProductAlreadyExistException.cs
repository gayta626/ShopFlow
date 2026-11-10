

namespace ShopFlow.Api.Exceptions;

public class ProductAlreadyExistException : Exception
{
    public ProductAlreadyExistException() : base("Sku da ton tai")
    {

    }
}