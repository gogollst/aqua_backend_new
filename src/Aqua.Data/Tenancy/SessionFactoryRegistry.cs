using System.Collections.Concurrent;
using Aqua.Contracts;
using Aqua.Data.Dialects;
using Aqua.Data.DependencyInjection;
using NHibernate;
using NHibernate.Cfg;

namespace Aqua.Data.Tenancy;

public sealed class SessionFactoryRegistry : IDisposable
{
    private readonly AquaDataOptions _options;
    private readonly IReadOnlyList<Action<Configuration>> _mappings;
    private readonly ConcurrentDictionary<string, Lazy<ISessionFactory>> _factories = new();

    public SessionFactoryRegistry(AquaDataOptions options, IEnumerable<Action<Configuration>> mappings)
    {
        _options = options;
        _mappings = mappings.ToList();
    }

    public ISessionFactory GetFor(TenantId tenant)
    {
        // Lazy<T> ensures ResolveTenantConfig is called at most once per tenant, even if
        // BuildSessionFactory() throws — the exception is cached and re-thrown on subsequent calls.
        var lazy = _factories.GetOrAdd(tenant.Value, _ => new Lazy<ISessionFactory>(() =>
        {
            var cfg = _options.ResolveTenantConfig(tenant);
            var nhCfg = new Configuration();
            nhCfg.SetProperty("connection.connection_string", cfg.ConnectionString);
            nhCfg.SetProperty("dialect", DialectFor(cfg.Dbms));
            nhCfg.SetProperty("connection.driver_class", DriverFor(cfg.Dbms));
            nhCfg.SetProperty("hbm2ddl.auto", "validate");
            foreach (var apply in _mappings) apply(nhCfg);
            return nhCfg.BuildSessionFactory();
        }));
        return lazy.Value;
    }

    private static string DialectFor(SupportedDbms dbms) => dbms switch
    {
        SupportedDbms.Postgres => typeof(PostgresExtendedDialect).AssemblyQualifiedName!,
        SupportedDbms.MsSql    => typeof(MsSql2012ExtendedDialect).AssemblyQualifiedName!,
        SupportedDbms.Oracle   => typeof(Oracle10gExtendedDialect).AssemblyQualifiedName!,
        _ => throw new ArgumentOutOfRangeException(nameof(dbms), dbms, null)
    };

    private static string DriverFor(SupportedDbms dbms) => dbms switch
    {
        SupportedDbms.Postgres => "NHibernate.Driver.NpgsqlDriver",
        SupportedDbms.MsSql    => "NHibernate.Driver.MicrosoftDataSqlClientDriver",
        SupportedDbms.Oracle   => "NHibernate.Driver.OracleManagedDataClientDriver",
        _ => throw new ArgumentOutOfRangeException(nameof(dbms), dbms, null)
    };

    public void Dispose()
    {
        foreach (var lazy in _factories.Values)
        {
            if (lazy.IsValueCreated) lazy.Value.Dispose();
        }
        _factories.Clear();
    }
}
