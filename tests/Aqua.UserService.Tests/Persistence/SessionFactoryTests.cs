using Aqua.UserService.Tests.TestSupport;
using FluentAssertions;
using NHibernate;
using Xunit;

namespace Aqua.UserService.Tests.Persistence;

[Collection(PostgresCollection.Name)]
public sealed class SessionFactoryTests
{
    private readonly PostgresFixture _fx;
    public SessionFactoryTests(PostgresFixture fx) => _fx = fx;

    [Fact]
    public void SessionFactory_opens_session_and_executes_select_1()
    {
        using var session = _fx.SessionFactory.OpenStatelessSession();
        var result = session.CreateSQLQuery("SELECT 1").UniqueResult<int>();
        result.Should().Be(1);
    }
}
