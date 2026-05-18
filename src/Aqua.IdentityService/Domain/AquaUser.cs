namespace Aqua.IdentityService.Domain;

/// <summary>
/// Maps to the legacy <c>aquaUser</c> table (profile portion). Auth columns
/// are on <see cref="AquaUserPassword"/> mapped to the same row.
/// </summary>
public class AquaUser
{
    public virtual int Id { get; protected set; }
    public virtual string UserName { get; set; } = default!;
    public virtual string? FirstName { get; set; }
    public virtual string? Surname { get; set; }
    public virtual string? Email { get; set; }
    public virtual DateTime? LastLogin { get; set; }
    public virtual string? Phone { get; set; }
    public virtual string? Position { get; set; }
    public virtual int UserStatus { get; set; }
    public virtual bool Deleted { get; set; }
    public virtual bool ServerAdmin { get; set; }
    public virtual bool PasswordExpiryExempt { get; set; }
}
