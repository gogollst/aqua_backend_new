using System.ComponentModel.DataAnnotations;

namespace Aqua.IdentityService.Configuration;

public sealed class LdapOptions
{
    [Required] public required string Host { get; init; }
    [Range(1, 65535)] public int Port { get; init; } = 389;
    public bool UseSsl { get; init; } = false;

    /// <summary>Base DN to search for users, e.g. "OU=Users,DC=acme,DC=corp".</summary>
    [Required] public required string BaseDn { get; init; }

    /// <summary>Attribute that maps the LDAP user to the AquaUser.UserName column. Usually "sAMAccountName".</summary>
    public string UserNameAttribute { get; init; } = "sAMAccountName";

    /// <summary>Optional bind user for searching. If null, anonymous bind is attempted.</summary>
    public string? BindDn { get; init; }
    public string? BindPassword { get; init; }
}
