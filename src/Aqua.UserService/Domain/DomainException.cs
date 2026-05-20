namespace Aqua.UserService.Domain;

public abstract class DomainException : Exception
{
    public string ErrorCode { get; }
    protected DomainException(string errorCode, string message, Exception? inner = null)
        : base(message, inner)
    {
        ErrorCode = errorCode;
    }
}
