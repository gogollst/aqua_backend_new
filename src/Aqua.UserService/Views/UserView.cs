using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Views;

public class UserView : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual long OwnerUserId { get; set; }
    public virtual long ProjectId { get; set; }
    public virtual string Name { get; set; } = "";
    public virtual int ViewType { get; set; }
    public virtual string? ConfigJson { get; set; }
    public virtual long Version { get; set; }
}
