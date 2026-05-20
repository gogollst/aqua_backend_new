using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Profiles;

public class WelcomePageConfig : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual long UserId { get; set; }
    public virtual string ConfigJson { get; set; } = "{}";
    public virtual long Version { get; set; }
}
