using Aqua.UserService.Domain;
using Aqua.UserService.Tenants;

namespace Aqua.UserService.Infrastructure;

/// <summary>
/// Resolves the numeric tenant id for the current request.
///
/// The <see cref="TenantContextMiddleware"/> only populates <see cref="ICurrentTenant.Slug"/>
/// from the X-Aqua-Tenant header (no DB lookup at request entry, to keep middleware fast).
/// Controllers that need the numeric id call <see cref="GetCurrentTenantIdAsync"/>; the
/// lookup is cached back into <see cref="ICurrentTenant"/> so a single request resolves at
/// most once.
/// </summary>
public interface ITenantResolver
{
    Task<long> GetCurrentTenantIdAsync();
}

public sealed class TenantResolver : ITenantResolver
{
    private readonly ICurrentTenant _tenant;
    private readonly ICustomerRepository _customers;

    public TenantResolver(ICurrentTenant tenant, ICustomerRepository customers)
    {
        _tenant = tenant;
        _customers = customers;
    }

    public async Task<long> GetCurrentTenantIdAsync()
    {
        if (!_tenant.IsResolved || _tenant.Slug is null)
            throw new ForbiddenException("tenant.missing", "Tenant context not set.");
        if (_tenant.Id.HasValue) return _tenant.Id.Value;

        var customer = await _customers.FindBySlugAsync(_tenant.Slug)
            ?? throw new NotFoundException("tenant.not-found", $"Tenant '{_tenant.Slug}' not found.");
        _tenant.Set(customer.Slug, customer.Id);
        return customer.Id;
    }
}
