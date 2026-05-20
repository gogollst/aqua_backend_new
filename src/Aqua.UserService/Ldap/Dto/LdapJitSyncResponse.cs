namespace Aqua.UserService.Ldap.Dto;

public sealed record LdapJitSyncResponse(
    long UserId,
    string Username,
    IReadOnlyList<string> Roles,
    long PermsBitset,
    bool IsNewUser);
