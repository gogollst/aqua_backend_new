using Aqua.UserService.Bookmarks.Dto;
using Aqua.UserService.Domain;

namespace Aqua.UserService.Bookmarks;

public interface IBookmarkManager
{
    Task<BookmarkDto> CreateAsync(CreateBookmarkRequest req, long ownerUserId, long customerId);
    Task<IReadOnlyList<BookmarkDto>> ListAsync(long ownerUserId, long projectId);
    Task DeleteAsync(long id, long ownerUserId, long customerId);
}

public sealed class BookmarkManager : IBookmarkManager
{
    private readonly IBookmarkRepository _repo;
    public BookmarkManager(IBookmarkRepository repo) => _repo = repo;

    public async Task<BookmarkDto> CreateAsync(CreateBookmarkRequest req, long ownerUserId, long customerId)
    {
        if (await _repo.FindAsync(ownerUserId, req.ProjectId, req.ItemType, req.ItemId) is not null)
            throw new ConflictException("bookmark.duplicate", "Bookmark already exists for this item.");

        var bm = new UserItemBookmark
        {
            CustomerId = customerId,
            UserId = ownerUserId,
            ProjectId = req.ProjectId,
            ItemType = req.ItemType,
            ItemId = req.ItemId,
            Label = req.Label,
            CreatedAt = DateTime.UtcNow,
        };
        await _repo.InsertAsync(bm);
        return new BookmarkDto(bm.Id, bm.ProjectId, bm.ItemType, bm.ItemId, bm.Label, bm.CreatedAt);
    }

    public async Task<IReadOnlyList<BookmarkDto>> ListAsync(long ownerUserId, long projectId)
    {
        var list = await _repo.ListByOwnerAsync(ownerUserId, projectId);
        return list
            .Select(b => new BookmarkDto(b.Id, b.ProjectId, b.ItemType, b.ItemId, b.Label, b.CreatedAt))
            .ToList();
    }

    public async Task DeleteAsync(long id, long ownerUserId, long customerId)
    {
        var bm = await _repo.FindByIdAsync(id)
            ?? throw new NotFoundException("bookmark.not-found", $"Bookmark {id} not found.");
        if (bm.UserId != ownerUserId)
            throw new ForbiddenException("bookmark.not-owner", "Not the owner.");
        await _repo.DeleteAsync(bm);
    }
}
