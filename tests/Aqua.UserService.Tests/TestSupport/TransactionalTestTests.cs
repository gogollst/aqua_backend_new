using FluentAssertions;
using NHibernate;
using Xunit;

namespace Aqua.UserService.Tests.TestSupport;

[Collection(PostgresCollection.Name)]
public sealed class TransactionalTestTests : TransactionalTest
{
    public TransactionalTestTests(PostgresFixture fx) : base(fx) {}

    [Fact]
    public void Session_writes_are_rolled_back_after_test()
    {
        Session.CreateSQLQuery("CREATE TEMP TABLE x (id int)").ExecuteUpdate();
        Session.CreateSQLQuery("INSERT INTO x VALUES (1)").ExecuteUpdate();
        var count = Session.CreateSQLQuery("SELECT COUNT(*) FROM x").UniqueResult<long>();
        count.Should().Be(1L);
    }
}
