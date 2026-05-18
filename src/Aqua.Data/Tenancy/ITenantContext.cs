using Aqua.Contracts;

namespace Aqua.Data.Tenancy;

/// <summary>
/// Per-request tenant identifier. Scope: AddScoped().
/// </summary>
public interface ITenantContext
{
    TenantId? Current { get; }
    void Set(TenantId tenant);
}
