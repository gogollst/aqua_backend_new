using Aqua.UserService.Tenants;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using FluentAssertions;
using Xunit;

namespace Aqua.UserService.Tests.Tenants;

[Collection(PostgresCollection.Name)]
public sealed class CustomerUserAssignmentRepositoryTests : TransactionalTest
{
    public CustomerUserAssignmentRepositoryTests(PostgresFixture fx) : base(fx) {}

    [Fact]
    public async Task AssignRole_and_GetRolesForUser_round_trip()
    {
        var userRepo = new UserRepository(Session);
        var custRepo = new CustomerRepository(Session);
        var cuaRepo  = new CustomerUserAssignmentRepository(Session);

        var customer = new TenantBuilder().WithSlug("test-roles").Build();
        await custRepo.InsertAsync(customer);
        Session.Flush();

        var user = new UserBuilder().WithUsername("u-cua").InCustomer(customer.Id).Build();
        await userRepo.InsertAsync(user);
        Session.Flush();

        await cuaRepo.AssignRolesAsync(
            customerId: customer.Id,
            userId: user.Id,
            roleIds: new[] { 42L, 99L });
        Session.Flush();

        var roles = await cuaRepo.GetRoleIdsAsync(customerId: customer.Id, userId: user.Id);
        roles.Should().BeEquivalentTo(new[] { 42L, 99L });
    }
}
