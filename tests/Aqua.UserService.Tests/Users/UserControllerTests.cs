using Aqua.UserService.Events;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users;
using Aqua.UserService.Users.Dto;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Users;

/// <summary>
/// Unit tests for <see cref="UserController"/>. WebApplicationFactory-based integration
/// tests (auth pipeline, permission filter, problem-details) are deferred to Task 32 once
/// the test host infrastructure exists — here we only verify that controller methods
/// route through to the managers/repos and emit the documented side-effects (events).
/// </summary>
public sealed class UserControllerTests
{
    private readonly IUserManager _users = Substitute.For<IUserManager>();
    private readonly ICustomerUserAssignmentRepository _cuas = Substitute.For<ICustomerUserAssignmentRepository>();
    private readonly IUserEventPublisher _publisher = Substitute.For<IUserEventPublisher>();
    private readonly ITenantResolver _tenants = Substitute.For<ITenantResolver>();
    private readonly UserController _sut;

    public UserControllerTests()
    {
        _tenants.GetCurrentTenantIdAsync().Returns(Task.FromResult(1L));
        _sut = new UserController(_users, _cuas, _publisher, _tenants);
    }

    [Fact]
    public async Task Get_returns_user_dto_from_manager()
    {
        var dto = new UserDto(17L, "alice", "Alice", "Anderson", "a@x.com",
            Phone: null, Position: null,
            Status: UserStatus.Active, ServerAdmin: false, Deleted: false,
            LdapDn: null, Version: 0L);
        _users.GetAsync(17L).Returns(Task.FromResult(dto));

        var result = await _sut.Get(17L);

        result.Result.Should().BeOfType<OkObjectResult>()
            .Subject.Value.Should().Be(dto);
    }

    [Fact]
    public async Task List_passes_tenant_skip_take_search_through()
    {
        var list = new List<UserDto>();
        _users.ListAsync(1L, 0, 50, null).Returns(Task.FromResult<IReadOnlyList<UserDto>>(list));

        var result = await _sut.List();

        result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(list);
        await _users.Received(1).ListAsync(1L, 0, 50, null);
    }

    [Fact]
    public async Task Create_returns_201_CreatedAtAction_with_dto()
    {
        var req = new CreateUserRequest("alice", "a@x.com", "Alice", "Anderson", null, null, null);
        var dto = new UserDto(42L, "alice", "Alice", "Anderson", "a@x.com",
            null, null, UserStatus.Active, false, false, null, 0L);
        _users.CreateAsync(req, 1L).Returns(Task.FromResult(dto));

        var result = await _sut.Create(req);

        var created = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(UserController.Get));
        created.RouteValues!["id"].Should().Be(42L);
        created.Value.Should().Be(dto);
    }

    [Fact]
    public async Task Delete_returns_NoContent_and_calls_soft_delete()
    {
        var result = await _sut.Delete(17L);

        result.Should().BeOfType<NoContentResult>();
        await _users.Received(1).SoftDeleteAsync(17L, 1L);
    }

    [Fact]
    public async Task AssignRoles_publishes_role_changed_with_sorted_diff()
    {
        _cuas.GetRoleIdsAsync(1L, 17L)
            .Returns(Task.FromResult<IReadOnlyList<long>>(new[] { 200L, 100L }));

        await _sut.AssignRoles(17L, new AssignUserRolesRequest(new[] { 300L, 100L }));

        await _cuas.Received(1).AssignRolesAsync(1L, 17L,
            Arg.Is<IReadOnlyCollection<long>>(c => c.SequenceEqual(new[] { 300L, 100L })));
        await _publisher.Received(1).PublishAsync(
            1L,
            "user.role-changed",
            Arg.Is<UserRoleChanged>(e =>
                e.UserId == 17L &&
                e.Source == "Admin" &&
                e.OldRoleIds.SequenceEqual(new[] { 100L, 200L }) &&
                e.NewRoleIds.SequenceEqual(new[] { 100L, 300L })),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Lookup_returns_users_from_manager()
    {
        var dtos = new List<UserDto>
        {
            new(1L, "a", "A", "A", "a@x.com", null, null, UserStatus.Active, false, false, null, 0L),
            new(2L, "b", "B", "B", "b@x.com", null, null, UserStatus.Active, false, false, null, 0L),
        };
        _users.GetByIdsAsync(Arg.Any<IReadOnlyCollection<long>>())
            .Returns(Task.FromResult<IReadOnlyList<UserDto>>(dtos));

        var result = await _sut.Lookup(new BulkUserLookupRequest(new[] { 1L, 2L }));

        result.Result.Should().BeOfType<OkObjectResult>().Subject.Value.Should().BeSameAs(dtos);
    }
}
