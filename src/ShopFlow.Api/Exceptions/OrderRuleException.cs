namespace ShopFlow.Api.Exceptions;

public class OrderRuleException : Exception
{
    public string Code { get; }

    public OrderRuleException(string code, string message) : base(message)
    {
        Code = code;
    }
}
