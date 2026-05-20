using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Ldap;

public class LdapGroupRoleMapping : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual string LdapGroupDn { get; set; } = "";
    public virtual long RoleId { get; set; }
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
