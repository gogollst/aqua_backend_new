namespace Aqua.UserService.Tenants.Dto;

public sealed record BootstrapTenantRequest(
    string Slug,
    string DisplayName,
    string? PrimaryDomain,
    BootstrapTenantAuth Auth,
    BootstrapTenantAdmin AdminUser,
    string DefaultRoles);

public sealed record BootstrapTenantAuth(
    TenantAuthMode Mode,
    string? LdapConfigJson,
    string? SamlConfigJson);

public sealed record BootstrapTenantAdmin(
    string Username,
    string Email,
    string FirstName,
    string Surname,
    string PasswordMode,            // "generate" | "provided"
    string? Password);
