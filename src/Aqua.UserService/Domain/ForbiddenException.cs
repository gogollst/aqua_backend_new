namespace Aqua.UserService.Domain;

public sealed class ForbiddenException : DomainException
{
    public ForbiddenException(string errorCode, string message) : base(errorCode, message) {}
}
