using Aqua.Contracts;

namespace Aqua.Data.Tenancy;

public sealed class TenantContext : ITenantContext
{
    public TenantId? Current { get; private set; }

    public void Set(TenantId tenant) => Current = tenant;
}
