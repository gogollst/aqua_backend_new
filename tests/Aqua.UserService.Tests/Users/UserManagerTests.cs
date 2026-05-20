using Aqua.UserService.Domain;
using Aqua.UserService.Events;
using Aqua.UserService.Tests.TestSupport;
using Aqua.UserService.Users;
using Aqua.UserService.Users.Dto;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Users;

public sealed class UserManagerTests
{
    private readonly IUserRepository _repo = Substitute.For<IUserRepository>();
    private readonly IUserEventPublisher _publisher = Substitute.For<IUserEventPublisher>();
    private readonly UserManager _sut;

    public UserManagerTests()
    {
        _sut = new UserManager(_repo, _publisher);
    }

    [Fact]
    public async Task Create_persists_user_and_returns_dto()
    {
        var req = new CreateUserRequest("alice", "alice@x.com", "Alice", "Anderson", null, null, null);
        _repo.FindByUsernameAsync("alice").Returns(Task.FromResult<User?>(null));
        _repo.FindByEmailAsync("alice@x.com").Returns(Task.FromResult<User?>(null));

        var dto = await _sut.CreateAsync(req, customerId: 1L);

        dto.Username.Should().Be("alice");
        await _repo.Received(1).InsertAsync(Arg.Is<User>(u =>
            u.Username == "alice" && u.Email == "alice@x.com" && u.Status == UserStatus.Active));
    }

    [Fact]
    public async Task Create_publishes_user_created_event()
    {
        var req = new CreateUserRequest("alice", "alice@x.com", "Alice", "Anderson", null, null, null);
        _repo.FindByUsernameAsync("alice").Returns(Task.FromResult<User?>(null));
        _repo.FindByEmailAsync("alice@x.com").Returns(Task.FromResult<User?>(null));

        await _sut.CreateAsync(req, customerId: 7L);

        await _publisher.Received(1).PublishAsync(
            7L, "user.created",
            Arg.Is<UserCreated>(e => e.Username == "alice" && e.Email == "alice@x.com" && !e.IsLdap),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_throws_conflict_on_duplicate_username()
    {
        _repo.FindByUsernameAsync("alice").Returns(Task.FromResult<User?>(new UserBuilder().Build()));
        var req = new CreateUserRequest("alice", "alice@x.com", "Alice", "Anderson", null, null, null);

        var act = () => _sut.CreateAsync(req, customerId: 1L);

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.ErrorCode == "user.username-taken");
        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<UserCreated>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_updates_supplied_fields_only()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        await _sut.PatchAsync(17L, new PatchUserRequest(
            FirstName: null,
            Surname:   "NewName",
            Email:     null,
            Phone:     "+49 123",
            Position:  null,
            Version:   existing.Version), customerId: 1L);

        existing.Surname.Should().Be("NewName");
        existing.Phone.Should().Be("+49 123");
        existing.FirstName.Should().Be("Alice");                // unchanged
    }

    [Fact]
    public async Task Patch_publishes_profile_changed_with_modified_field_list()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        await _sut.PatchAsync(17L, new PatchUserRequest(
            FirstName: null, Surname: "NewName", Email: null, Phone: "+49 123",
            Position: null, Version: existing.Version), customerId: 4L);

        await _publisher.Received(1).PublishAsync(
            4L, "user.profile-changed",
            Arg.Is<UserProfileChanged>(e =>
                e.UserId == 17L &&
                e.ChangedFields.Count == 2 &&
                e.ChangedFields.Contains("Surname") &&
                e.ChangedFields.Contains("Phone")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_with_no_changes_does_not_publish()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        await _sut.PatchAsync(17L, new PatchUserRequest(
            null, null, null, null, null, Version: existing.Version), customerId: 1L);

        await _publisher.DidNotReceive().PublishAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<UserProfileChanged>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Patch_throws_stale_version()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        existing.Version = 5;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        var act = () => _sut.PatchAsync(17L, new PatchUserRequest(
            null, "X", null, null, null, Version: 4), customerId: 1L);

        await act.Should().ThrowAsync<StaleVersionException>();
    }

    [Fact]
    public async Task SoftDelete_sets_deleted_flag_and_disables_user()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        await _sut.SoftDeleteAsync(17L, customerId: 1L);

        existing.Deleted.Should().BeTrue();
        existing.Status.Should().Be(UserStatus.Disabled);
    }

    [Fact]
    public async Task SoftDelete_publishes_disabled_then_deleted()
    {
        var existing = new UserBuilder().Build();
        existing.Id = 17L;
        _repo.FindByIdAsync(17L).Returns(Task.FromResult<User?>(existing));

        await _sut.SoftDeleteAsync(17L, customerId: 9L);

        Received.InOrder(() =>
        {
            _publisher.PublishAsync(
                9L, "user.disabled",
                Arg.Is<UserDisabled>(e => e.UserId == 17L && e.Reason == "AdminAction"),
                Arg.Any<CancellationToken>());
            _publisher.PublishAsync(
                9L, "user.deleted",
                Arg.Is<UserDeleted>(e => e.UserId == 17L),
                Arg.Any<CancellationToken>());
        });
    }
}
