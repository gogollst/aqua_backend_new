using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Profiles;

public class UserAssignedProfile : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual long UserId { get; set; }
    public virtual string ProfileType { get; set; } = "";
    public virtual DateTime AssignedAt { get; set; }
}
