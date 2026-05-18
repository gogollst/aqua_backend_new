namespace Aqua.IdentityService.Domain;

/// <summary>
/// Maps to the legacy <c>aquaUser</c> table (auth portion). The Id is shared with <see cref="AquaUser"/>.
/// </summary>
public class AquaUserPassword
{
    public virtual int Id { get; protected set; }
    public virtual string? ClearTextPassword { get; set; }   // legacy field, must remain mapped
    public virtual string? Password { get; set; }            // BCrypt hash, max 172 chars
    public virtual DateTime? LastPasswordChange { get; set; }
    public virtual string? PasswordHistory { get; set; }     // semicolon-separated BCrypt hashes
    public virtual int FailedLoginCount { get; set; }
    public virtual DateTime? LockedUntil { get; set; }
}
