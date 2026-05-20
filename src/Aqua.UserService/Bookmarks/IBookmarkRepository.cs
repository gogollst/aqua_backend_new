namespace Aqua.UserService.Bookmarks;

public interface IBookmarkRepository
{
    Task<UserItemBookmark?> FindByIdAsync(long id);
    Task<UserItemBookmark?> FindAsync(long userId, long projectId, string itemType, long itemId);
    Task<IReadOnlyList<UserItemBookmark>> ListByOwnerAsync(long userId, long projectId);
    Task InsertAsync(UserItemBookmark bm);
    Task DeleteAsync(UserItemBookmark bm);
}
