namespace Aqua.UserService.Views.Dto;

public sealed record UserViewDto(
    long Id,
    long OwnerUserId,
    long ProjectId,
    string Name,
    int ViewType,
    string? ConfigJson,
    bool IsFavorite,
    long Version);

public sealed record CreateUserViewRequest(
    long ProjectId,
    string Name,
    int ViewType,
    string? ConfigJson);
