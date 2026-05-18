using Aqua.IdentityService.Configuration;
using Microsoft.Extensions.Options;

namespace Aqua.IdentityService.Authentication;

/// <summary>
/// Top-level <see cref="IAuthenticationProvider"/> that delegates to the database or LDAP
/// provider based on configuration. Mounted as the public dependency in DI.
/// </summary>
public sealed class CompositeAuthenticationProvider : IAuthenticationProvider
{
    private readonly AuthenticationOptions _options;
    private readonly IAuthenticationProvider _db;
    private readonly IAuthenticationProvider _ldap;

    public CompositeAuthenticationProvider(
        IOptions<AuthenticationOptions> options,
        DatabaseAuthenticationProvider db,
        LdapAuthenticationProvider ldap)
    {
        _options = options.Value;
        _db = db;
        _ldap = ldap;
    }

    // Constructor for testing — accepts any IAuthenticationProvider implementation.
    public CompositeAuthenticationProvider(
        IOptions<AuthenticationOptions> options,
        IAuthenticationProvider db,
        IAuthenticationProvider ldap)
    {
        _options = options.Value;
        _db = db;
        _ldap = ldap;
    }

    public Task<AuthenticationResult> AuthenticateAsync(string userName, string password, CancellationToken ct = default)
    {
        if (_options.LdapOnlyUsernames.Contains(userName))
            return _ldap.AuthenticateAsync(userName, password, ct);

        return _options.Mode switch
        {
            AuthenticationMode.DatabaseOnly => _db.AuthenticateAsync(userName, password, ct),
            AuthenticationMode.LdapOnly => _ldap.AuthenticateAsync(userName, password, ct),
            AuthenticationMode.BothPreferDatabase => TryThenFallbackAsync(_db, _ldap, userName, password, ct),
            AuthenticationMode.BothPreferLdap => TryThenFallbackAsync(_ldap, _db, userName, password, ct),
            _ => Task.FromResult(AuthenticationResult.Fail(AuthenticationFailureReason.InternalError, "Unknown mode")),
        };
    }

    private static async Task<AuthenticationResult> TryThenFallbackAsync(
        IAuthenticationProvider primary, IAuthenticationProvider fallback,
        string un, string pw, CancellationToken ct)
    {
        var first = await primary.AuthenticateAsync(un, pw, ct);
        return first.Success ? first : await fallback.AuthenticateAsync(un, pw, ct);
    }
}
