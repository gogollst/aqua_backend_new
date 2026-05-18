using Aqua.Contracts;

namespace Aqua.Data.DependencyInjection;

public sealed class AquaDataOptions
{
    public Func<TenantId, TenantDbConfig> ResolveTenantConfig { get; set; } = _ => throw new InvalidOperationException("ResolveTenantConfig must be set.");
    public TimeSpan SessionFactoryCacheTtl { get; set; } = TimeSpan.FromHours(1);
}

public sealed record TenantDbConfig(SupportedDbms Dbms, string ConnectionString);

public enum SupportedDbms { Postgres, MsSql, Oracle }
