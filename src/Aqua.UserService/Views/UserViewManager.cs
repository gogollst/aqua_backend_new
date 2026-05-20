using Aqua.UserService.Domain;
using Aqua.UserService.Views.Dto;

namespace Aqua.UserService.Views;

public interface IUserViewManager
{
    Task<UserViewDto> CreateAsync(CreateUserViewRequest req, long ownerUserId, long customerId);
    Task<IReadOnlyList<UserViewDto>> ListAsync(long ownerUserId, long projectId);
    Task DeleteAsync(long viewId, long ownerUserId, long customerId);
    Task SetFavoriteAsync(long viewId, long ownerUserId, bool isFavorite, long customerId);
}

public sealed class UserViewManager : IUserViewManager
{
    private readonly IUserViewRepository _repo;
    public UserViewManager(IUserViewRepository repo) => _repo = repo;

    public async Task<UserViewDto> CreateAsync(CreateUserViewRequest req, long ownerUserId, long customerId)
    {
        var v = new UserView
        {
            CustomerId = customerId,
            OwnerUserId = ownerUserId,
            ProjectId = req.ProjectId,
            Name = req.Name,
            ViewType = req.ViewType,
            ConfigJson = req.ConfigJson,
        };
        await _repo.InsertAsync(v);
        return new UserViewDto(v.Id, v.OwnerUserId, v.ProjectId, v.Name, v.ViewType, v.ConfigJson, IsFavorite: false, v.Version);
    }

    public async Task<IReadOnlyList<UserViewDto>> ListAsync(long ownerUserId, long projectId)
    {
        var list = await _repo.ListByOwnerAsync(ownerUserId, projectId);
        var result = new List<UserViewDto>(list.Count);
        foreach (var v in list)
        {
            var fav = await _repo.GetFavoriteAsync(ownerUserId, v.Id);
            result.Add(new UserViewDto(v.Id, v.OwnerUserId, v.ProjectId, v.Name, v.ViewType, v.ConfigJson, fav is not null, v.Version));
        }
        return result;
    }

    public async Task DeleteAsync(long viewId, long ownerUserId, long customerId)
    {
        var v = await _repo.FindByIdAsync(viewId)
            ?? throw new NotFoundException("view.not-found", $"View {viewId} not found.");
        if (v.OwnerUserId != ownerUserId)
            throw new ForbiddenException("view.not-owner", "Only the owner may delete a view.");
        await _repo.DeleteAsync(v);
    }

    public async Task SetFavoriteAsync(long viewId, long ownerUserId, bool isFavorite, long customerId)
    {
        _ = await _repo.FindByIdAsync(viewId)
            ?? throw new NotFoundException("view.not-found", $"View {viewId} not found.");
        var existing = await _repo.GetFavoriteAsync(ownerUserId, viewId);
        if (isFavorite && existing is null)
        {
            await _repo.InsertFavoriteAsync(new UserViewFavorite { UserId = ownerUserId, ViewId = viewId });
        }
        else if (!isFavorite && existing is not null)
        {
            await _repo.DeleteFavoriteAsync(existing);
        }
    }
}
