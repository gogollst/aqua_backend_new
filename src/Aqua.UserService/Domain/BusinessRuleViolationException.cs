namespace Aqua.UserService.Domain;

public sealed class BusinessRuleViolationException : DomainException
{
    public BusinessRuleViolationException(string errorCode, string message) : base(errorCode, message) {}
}
