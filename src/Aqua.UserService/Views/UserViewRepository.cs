using NHibernate;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Aqua.UserService.Views;

public sealed class UserViewRepository : IUserViewRepository
{
    private readonly ISession _session;
    public UserViewRepository(ISession session) => _session = session;

    public async Task<UserView?> FindByIdAsync(long id) =>
        await _session.GetAsync<UserView>(id);

    public async Task<IReadOnlyList<UserView>> ListByOwnerAsync(long ownerUserId, long projectId) =>
        await _session.Query<UserView>()
            .Where(v => v.OwnerUserId == ownerUserId && v.ProjectId == projectId)
            .OrderBy(v => v.Name)
            .ToListAsync();

    public Task InsertAsync(UserView view) => _session.SaveAsync(view);
    public Task DeleteAsync(UserView view) => _session.DeleteAsync(view);

    public async Task<UserViewFavorite?> GetFavoriteAsync(long userId, long viewId) =>
        await _session.Query<UserViewFavorite>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ViewId == viewId);

    public Task InsertFavoriteAsync(UserViewFavorite fav) => _session.SaveAsync(fav);
    public Task DeleteFavoriteAsync(UserViewFavorite fav) => _session.DeleteAsync(fav);
}
