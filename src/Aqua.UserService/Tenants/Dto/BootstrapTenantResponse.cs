namespace Aqua.UserService.Tenants.Dto;

public sealed record BootstrapTenantResponse(
    long TenantId,
    string TenantSlug,
    long AdminUserId,
    string AdminUsername,
    string? InitialPassword,
    bool Skipped,
    int RolesCreated);
