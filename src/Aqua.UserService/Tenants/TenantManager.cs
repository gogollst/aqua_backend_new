using Aqua.UserService.Domain;
using Aqua.UserService.Tenants.Dto;

namespace Aqua.UserService.Tenants;

public interface ITenantManager
{
    Task<TenantSettingsDto> GetSettingsAsync(long tenantId);
    Task<TenantSettingsDto> PatchSettingsAsync(long tenantId, PatchTenantSettingsRequest req);
}

public sealed class TenantManager : ITenantManager
{
    private readonly ICustomerRepository _repo;

    public TenantManager(ICustomerRepository repo) => _repo = repo;

    public async Task<TenantSettingsDto> GetSettingsAsync(long tenantId)
    {
        var c = await _repo.FindByIdAsync(tenantId)
            ?? throw NotFoundException.ForTenant(tenantId);
        return ToDto(c);
    }

    public async Task<TenantSettingsDto> PatchSettingsAsync(long tenantId, PatchTenantSettingsRequest req)
    {
        var c = await _repo.FindByIdAsync(tenantId)
            ?? throw NotFoundException.ForTenant(tenantId);
        if (c.Version != req.Version)
            throw new StaleVersionException(c.Version,
                $"Tenant version {req.Version} is stale (current = {c.Version}).");

        if (req.DisplayName    is not null) c.DisplayName    = req.DisplayName;
        if (req.AuthMode       is { } am)   c.AuthMode       = am;
        if (req.AuthConfigJson is not null) c.AuthConfigJson = req.AuthConfigJson;
        return ToDto(c);
    }

    private static TenantSettingsDto ToDto(Customer c) => new(
        c.Id, c.Slug, c.DisplayName, c.PrimaryDomain, c.AuthMode, c.AuthConfigJson, c.Version);
}
