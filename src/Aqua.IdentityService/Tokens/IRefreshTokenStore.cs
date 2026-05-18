using Aqua.Data.Sessions;
using Aqua.IdentityService.Domain;
using NHibernate.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Aqua.IdentityService.Tokens;

public interface IRefreshTokenStore
{
    Task SaveAsync(string plaintextToken, int userId, string tenantId, DateTimeOffset expiresAt, string? clientIp, CancellationToken ct = default);

    /// <summary>Validates the token and atomically rotates it: returns the existing entity (if valid) and marks it consumed.</summary>
    Task<RefreshToken?> ConsumeAsync(string plaintextToken, Guid newTokenId, CancellationToken ct = default);

    /// <summary>Revokes all tokens for a user (e.g. on logout-all / password change).</summary>
    Task RevokeAllForUserAsync(int userId, string reason, CancellationToken ct = default);
}

public sealed class RefreshTokenStore : IRefreshTokenStore
{
    private readonly ISessionScope _scope;
    public RefreshTokenStore(ISessionScope scope) => _scope = scope;

    public async Task SaveAsync(string plaintextToken, int userId, string tenantId, DateTimeOffset expiresAt, string? clientIp, CancellationToken ct = default)
    {
        var entity = new RefreshToken
        {
            UserId = userId,
            TenantId = tenantId,
            TokenHash = HashToken(plaintextToken),
            ExpiresAt = expiresAt,
            ClientIp = clientIp,
        };
        await _scope.Session.SaveAsync(entity, ct);
    }

    public async Task<RefreshToken?> ConsumeAsync(string plaintextToken, Guid newTokenId, CancellationToken ct = default)
    {
        var hash = HashToken(plaintextToken);
        var found = await _scope.Session.Query<RefreshToken>()
            .FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (found is null) return null;
        if (found.RevokedAt is not null) return null;
        if (found.RotatedToTokenId is not null)
        {
            // REUSE detected — revoke entire family for safety.
            await RevokeAllForUserAsync(found.UserId, "refresh-token-reuse-detected", ct);
            return null;
        }
        if (found.ExpiresAt < DateTimeOffset.UtcNow) return null;

        found.RotatedToTokenId = newTokenId;
        await _scope.Session.UpdateAsync(found, ct);
        return found;
    }

    public async Task RevokeAllForUserAsync(int userId, string reason, CancellationToken ct = default)
    {
        var active = await _scope.Session.Query<RefreshToken>()
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in active)
        {
            t.RevokedAt = DateTimeOffset.UtcNow;
            t.RevocationReason = reason;
            await _scope.Session.UpdateAsync(t, ct);
        }
    }

    private static string HashToken(string plaintext)
    {
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
