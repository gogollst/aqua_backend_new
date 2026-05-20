namespace Aqua.UserService.Ldap.Dto;

public sealed record LdapJitSyncRequest(
    string CustomerSlug,
    string LdapDn,
    string Username,
    string Email,
    string FirstName,
    string Surname,
    IReadOnlyList<string> Groups);
