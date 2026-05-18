using NHibernate;

namespace Aqua.Data.Sessions;

/// <summary>
/// Per-request NHibernate session, scoped to current tenant.
/// </summary>
public interface ISessionScope : IDisposable
{
    ISession Session { get; }
}
