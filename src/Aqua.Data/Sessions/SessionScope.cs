using Aqua.Data.Tenancy;
using NHibernate;

namespace Aqua.Data.Sessions;

public sealed class SessionScope : ISessionScope
{
    private readonly ISession _session;
    private bool _disposed;

    public SessionScope(ITenantContext tenantContext, SessionFactoryRegistry registry)
    {
        if (tenantContext.Current is null)
            throw new InvalidOperationException("No tenant in scope. Add TenantMiddleware to the pipeline.");
        _session = registry.GetFor(tenantContext.Current.Value).OpenSession();
    }

    public ISession Session => _session;

    public void Dispose()
    {
        if (_disposed) return;
        _session.Dispose();
        _disposed = true;
    }
}
