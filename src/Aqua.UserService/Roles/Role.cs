using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Roles;

public class Role : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual string Name { get; set; } = "";
    public virtual string? Description { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual bool AvailableInProject { get; set; } = true;
    public virtual bool AvailableInCustomer { get; set; } = true;
    public virtual bool IsDefault { get; set; }
    public virtual PermissionBitset Permissions { get; set; } = PermissionBitset.None;
    public virtual string? PermVersion { get; set; }
    public virtual long Version { get; set; }
}
