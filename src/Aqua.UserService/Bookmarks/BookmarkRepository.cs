using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Bookmarks;

public sealed class BookmarkRepository : IBookmarkRepository
{
    private readonly ISession _session;
    public BookmarkRepository(ISession session) => _session = session;

    public async Task<UserItemBookmark?> FindByIdAsync(long id) =>
        await _session.GetAsync<UserItemBookmark>(id);

    public async Task<UserItemBookmark?> FindAsync(long userId, long projectId, string itemType, long itemId) =>
        await _session.Query<UserItemBookmark>().FirstOrDefaultAsync(b =>
            b.UserId == userId && b.ProjectId == projectId &&
            b.ItemType == itemType && b.ItemId == itemId);

    public async Task<IReadOnlyList<UserItemBookmark>> ListByOwnerAsync(long userId, long projectId) =>
        await _session.Query<UserItemBookmark>()
            .Where(b => b.UserId == userId && b.ProjectId == projectId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public Task InsertAsync(UserItemBookmark bm) => _session.SaveAsync(bm);
    public Task DeleteAsync(UserItemBookmark bm) => _session.DeleteAsync(bm);
}
