namespace EVShop.Ordering.Domain.Exceptions;

public class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message)
    {
    }
}
