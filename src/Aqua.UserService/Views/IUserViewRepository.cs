namespace Aqua.UserService.Views;

public interface IUserViewRepository
{
    Task<UserView?> FindByIdAsync(long id);
    Task<IReadOnlyList<UserView>> ListByOwnerAsync(long ownerUserId, long projectId);
    Task InsertAsync(UserView view);
    Task DeleteAsync(UserView view);

    Task<UserViewFavorite?> GetFavoriteAsync(long userId, long viewId);
    Task InsertFavoriteAsync(UserViewFavorite fav);
    Task DeleteFavoriteAsync(UserViewFavorite fav);
}
