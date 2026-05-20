namespace Aqua.UserService.Tenants.Dto;

public sealed record TenantSettingsDto(
    long           Id,
    string         Slug,
    string         DisplayName,
    string?        PrimaryDomain,
    TenantAuthMode AuthMode,
    string?        AuthConfigJson,
    long           Version);

public sealed record PatchTenantSettingsRequest(
    string?         DisplayName,
    TenantAuthMode? AuthMode,
    string?         AuthConfigJson,
    long            Version);
