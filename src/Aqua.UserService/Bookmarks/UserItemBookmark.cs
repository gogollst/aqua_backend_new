using Aqua.UserService.Persistence.Conventions;

namespace Aqua.UserService.Bookmarks;

public class UserItemBookmark : ITenantFilteredEntity
{
    public virtual long Id { get; set; }
    public virtual long CustomerId { get; set; }
    public virtual long UserId { get; set; }
    public virtual long ProjectId { get; set; }
    public virtual string ItemType { get; set; } = "";
    public virtual long ItemId { get; set; }
    public virtual string? Label { get; set; }
    public virtual DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
