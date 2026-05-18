using Aqua.Contracts;
using Aqua.Data.DependencyInjection;
using Aqua.Data.Tenancy;
using FluentAssertions;
using Xunit;

namespace Aqua.Data.Tests;

public class SessionFactoryRegistryTests
{
    [Fact]
    public void GetForTenant_UnknownTenant_Throws()
    {
        var opts = new AquaDataOptions { ResolveTenantConfig = _ => throw new InvalidOperationException("not found") };
        var registry = new SessionFactoryRegistry(opts, mappings: new List<Action<NHibernate.Cfg.Configuration>>());

        var act = () => registry.GetFor(new TenantId("nope"));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GetForTenant_CallsResolveOnlyOnce()
    {
        int calls = 0;
        var opts = new AquaDataOptions
        {
            ResolveTenantConfig = _ =>
            {
                calls++;
                // Use a bogus Postgres connection string; factory building will fail — that's OK.
                return new TenantDbConfig(SupportedDbms.Postgres, "Host=localhost;Database=fake;");
            }
        };
        var registry = new SessionFactoryRegistry(opts, mappings: new List<Action<NHibernate.Cfg.Configuration>>());

        try { registry.GetFor(new TenantId("acme")); } catch { /* connect will fail — OK */ }
        try { registry.GetFor(new TenantId("acme")); } catch { /* connect will fail — OK */ }

        calls.Should().Be(1, "second call should use the cached factory");
    }
}
