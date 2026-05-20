using Aqua.UserService.Bookmarks;
using Aqua.UserService.Bookmarks.Dto;
using Aqua.UserService.Domain;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Bookmarks;

public sealed class BookmarkManagerTests
{
    private readonly IBookmarkRepository _repo = Substitute.For<IBookmarkRepository>();
    private readonly BookmarkManager _sut;
    public BookmarkManagerTests() => _sut = new BookmarkManager(_repo);

    [Fact]
    public async Task Create_inserts_bookmark()
    {
        _repo.FindAsync(17L, 5L, "Defect", 42L)
            .Returns(Task.FromResult<UserItemBookmark?>(null));

        var dto = await _sut.CreateAsync(new CreateBookmarkRequest(
            ProjectId: 5L,
            ItemType: "Defect",
            ItemId: 42L,
            Label: "Critical bug"), ownerUserId: 17L, customerId: 1L);

        dto.ItemId.Should().Be(42L);
        await _repo.Received(1).InsertAsync(Arg.Any<UserItemBookmark>());
    }

    [Fact]
    public async Task Create_duplicate_throws_conflict()
    {
        _repo.FindAsync(17L, 5L, "Defect", 42L)
            .Returns(Task.FromResult<UserItemBookmark?>(new UserItemBookmark()));

        var act = () => _sut.CreateAsync(
            new CreateBookmarkRequest(5L, "Defect", 42L, "Crit"),
            ownerUserId: 17L, customerId: 1L);

        await act.Should().ThrowAsync<ConflictException>()
            .Where(e => e.ErrorCode == "bookmark.duplicate");
    }

    [Fact]
    public async Task Delete_not_owner_throws_forbidden()
    {
        var bm = new UserItemBookmark { Id = 7L, UserId = 99L };
        _repo.FindByIdAsync(7L).Returns(Task.FromResult<UserItemBookmark?>(bm));

        var act = () => _sut.DeleteAsync(7L, ownerUserId: 17L, customerId: 1L);

        await act.Should().ThrowAsync<ForbiddenException>()
            .Where(e => e.ErrorCode == "bookmark.not-owner");
    }
}
