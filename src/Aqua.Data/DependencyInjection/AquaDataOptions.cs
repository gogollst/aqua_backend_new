using Aqua.Contracts;

namespace Aqua.Data.DependencyInjection;

public sealed class AquaDataOptions
{
    public required Func<TenantId, TenantDbConfig> ResolveTenantConfig { get; init; }
    public TimeSpan SessionFactoryCacheTtl { get; init; } = TimeSpan.FromHours(1);
}

public sealed record TenantDbConfig(SupportedDbms Dbms, string ConnectionString);

public enum SupportedDbms { Postgres, MsSql, Oracle }
