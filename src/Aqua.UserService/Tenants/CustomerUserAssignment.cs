using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Tenants;

public class CustomerUserAssignment : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual long UserId { get; set; }
    public virtual long RoleId { get; set; }
    public virtual long Version { get; set; }
}
