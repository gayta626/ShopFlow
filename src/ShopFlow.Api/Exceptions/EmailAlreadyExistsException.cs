
namespace ShopFlow.Api.Exceptions;


public class EmailAlreadyExistsException : Exception
{
    public EmailAlreadyExistsException() : base("Email da duoc su dung")
    {

    }
}