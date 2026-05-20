namespace Aqua.UserService.Domain;

public sealed class ConflictException : DomainException
{
    public ConflictException(string errorCode, string message) : base(errorCode, message) {}
}
