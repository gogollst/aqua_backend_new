using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class TenantFilterTests
{
    private readonly PostgresFixture _fx;
    public TenantFilterTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public void SessionFactory_has_tenant_filter_definition()
    {
        _fx.SessionFactory.DefinedFilterNames.Should().Contain("tenant_filter");
        var def = _fx.SessionFactory.GetFilterDefinition("tenant_filter");
        def.Should().NotBeNull();
        def.ParameterTypes.Should().ContainKey("tenantId");
    }

    [Fact]
    public void Session_can_enable_filter_with_tenant_parameter()
    {
        using var session = _fx.SessionFactory.OpenSession();
        var filter = session.EnableFilter("tenant_filter");
        filter.SetParameter("tenantId", 42L);
        var enabled = session.GetEnabledFilter("tenant_filter");
        enabled.Should().NotBeNull();
    }
}
