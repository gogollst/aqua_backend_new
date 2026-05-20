using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Users;

[Collection(PostgresCollection.Name)]
public sealed class UserRepositoryTests : TransactionalTest
{
    public UserRepositoryTests(PostgresFixture fx) : base(fx) {}

    [Fact]
    public async Task Insert_and_FindById_round_trip()
    {
        var repo = new UserRepository(Session);
        var u = new UserBuilder().WithUsername("bob").InCustomer(1L).Build();
        await repo.InsertAsync(u);
        Session.Flush();

        var loaded = await repo.FindByIdAsync(u.Id);
        loaded.Should().NotBeNull();
        loaded!.Username.Should().Be("bob");
    }

    [Fact]
    public async Task FindByLdapDn_returns_user_when_exists()
    {
        var repo = new UserRepository(Session);
        var u = new UserBuilder().WithUsername("ldap-bob")
            .WithLdapDn("uid=bob,dc=acme,dc=com").InCustomer(1L).Build();
        await repo.InsertAsync(u);
        Session.Flush();

        var found = await repo.FindByLdapDnAsync(1L, "uid=bob,dc=acme,dc=com");
        found.Should().NotBeNull();
        found!.Username.Should().Be("ldap-bob");
    }
}
