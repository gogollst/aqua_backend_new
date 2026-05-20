using Aqua.UserService.Domain;
using Aqua.UserService.Views;
using Aqua.UserService.Views.Dto;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Views;

public sealed class UserViewManagerTests
{
    private readonly IUserViewRepository _repo = Substitute.For<IUserViewRepository>();
    private readonly UserViewManager _sut;
    public UserViewManagerTests() => _sut = new UserViewManager(_repo);

    [Fact]
    public async Task Create_persists_view_for_owner_and_project()
    {
        var dto = await _sut.CreateAsync(new CreateUserViewRequest(
            ProjectId: 5L,
            Name: "My open defects",
            ViewType: 1,
            ConfigJson: """{"filter":"status=open"}"""),
            ownerUserId: 17L, customerId: 1L);

        dto.OwnerUserId.Should().Be(17L);
        dto.ProjectId.Should().Be(5L);
        dto.Name.Should().Be("My open defects");
        await _repo.Received(1).InsertAsync(Arg.Any<UserView>());
    }

    [Fact]
    public async Task Delete_owned_view_succeeds()
    {
        var v = new UserView { Id = 42L, OwnerUserId = 17L, CustomerId = 1L };
        _repo.FindByIdAsync(42L).Returns(Task.FromResult<UserView?>(v));

        await _sut.DeleteAsync(42L, ownerUserId: 17L, customerId: 1L);

        await _repo.Received(1).DeleteAsync(v);
    }

    [Fact]
    public async Task Delete_others_view_throws_forbidden()
    {
        var v = new UserView { Id = 42L, OwnerUserId = 99L, CustomerId = 1L };
        _repo.FindByIdAsync(42L).Returns(Task.FromResult<UserView?>(v));

        var act = () => _sut.DeleteAsync(42L, ownerUserId: 17L, customerId: 1L);

        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(e => e.ErrorCode == "view.not-owner");
    }

    [Fact]
    public async Task SetFavorite_inserts_when_missing()
    {
        var v = new UserView { Id = 42L, OwnerUserId = 17L, CustomerId = 1L };
        _repo.FindByIdAsync(42L).Returns(Task.FromResult<UserView?>(v));
        _repo.GetFavoriteAsync(17L, 42L).Returns(Task.FromResult<UserViewFavorite?>(null));

        await _sut.SetFavoriteAsync(42L, ownerUserId: 17L, isFavorite: true, customerId: 1L);

        await _repo.Received(1).InsertFavoriteAsync(
            Arg.Is<UserViewFavorite>(f => f.UserId == 17L && f.ViewId == 42L));
    }
}
