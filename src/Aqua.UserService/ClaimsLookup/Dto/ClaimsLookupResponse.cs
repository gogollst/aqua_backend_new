namespace Aqua.UserService.ClaimsLookup.Dto;

public sealed record ClaimsLookupResponse(
    string Sub,
    string Name,
    string Email,
    long   TenantId,
    string TenantSlug,
    bool   IsActive,
    bool   ServerAdmin,
    IReadOnlyList<string> Roles,
    long   PermsBitset);
