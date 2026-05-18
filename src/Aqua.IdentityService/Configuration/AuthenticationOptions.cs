namespace Aqua.IdentityService.Configuration;

public enum AuthenticationMode
{
    DatabaseOnly,
    LdapOnly,
    BothPreferLdap,
    BothPreferDatabase,
}

public sealed class AuthenticationOptions
{
    public AuthenticationMode Mode { get; init; } = AuthenticationMode.DatabaseOnly;

    /// <summary>If non-empty, only these usernames go through LDAP; everyone else through DB.</summary>
    public IReadOnlyList<string> LdapOnlyUsernames { get; init; } = Array.Empty<string>();
}
