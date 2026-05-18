namespace Aqua.IdentityService.Domain;

public class RefreshToken
{
    public virtual Guid Id { get; protected set; } = Guid.NewGuid();
    public virtual int UserId { get; set; }
    public virtual string TenantId { get; set; } = default!;

    /// <summary>SHA-256 hash of the actual token string. The plaintext is never persisted.</summary>
    public virtual string TokenHash { get; set; } = default!;

    public virtual DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public virtual DateTimeOffset ExpiresAt { get; set; }

    /// <summary>If non-null: this token was used; the new token's id is here. Triggers reuse-detection.</summary>
    public virtual Guid? RotatedToTokenId { get; set; }

    public virtual DateTimeOffset? RevokedAt { get; set; }
    public virtual string? RevocationReason { get; set; }

    /// <summary>For audit: which IP issued / consumed this token.</summary>
    public virtual string? ClientIp { get; set; }
}
