namespace Aqua.UserService.Domain;

public sealed class NotFoundException : DomainException
{
    public NotFoundException(string errorCode, string message) : base(errorCode, message) {}
    public static NotFoundException ForUser(long id) => new("user.not-found", $"User {id} not found.");
    public static NotFoundException ForRole(long id) => new("role.not-found", $"Role {id} not found.");
    public static NotFoundException ForTenant(long id) => new("tenant.not-found", $"Tenant {id} not found.");
}
