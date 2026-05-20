namespace Aqua.UserService.Users;

public class User
{
    public virtual long Id { get; set; }
    public virtual string Username { get; set; } = "";
    public virtual string FirstName { get; set; } = "";
    public virtual string Surname { get; set; } = "";
    public virtual string Email { get; set; } = "";
    public virtual string? Phone { get; set; }
    public virtual string? Position { get; set; }
    public virtual long? PictureScreenshotId { get; set; }
    public virtual UserStatus Status { get; set; } = UserStatus.Active;
    public virtual bool ServerAdmin { get; set; }
    public virtual bool Deleted { get; set; }
    public virtual long? LastLoginUnixSeconds { get; set; }
    public virtual string? UserData { get; set; }
    public virtual string? UserDataWeb { get; set; }
    public virtual string? LdapDn { get; set; }

    /// <summary>
    /// Customer (tenant) the user primarily belongs to.
    /// Membership is also tracked in <c>CustomerUserAssignment</c>; this column is denormalised
    /// for tenant_filter participation.
    /// </summary>
    public virtual long CustomerIdHint { get; set; }

    public virtual long Version { get; set; }
}
