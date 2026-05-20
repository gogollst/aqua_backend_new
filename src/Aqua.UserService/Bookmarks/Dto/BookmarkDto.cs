namespace Aqua.UserService.Bookmarks.Dto;

public sealed record BookmarkDto(
    long Id,
    long ProjectId,
    string ItemType,
    long ItemId,
    string? Label,
    DateTime CreatedAt);

public sealed record CreateBookmarkRequest(
    long ProjectId,
    string ItemType,
    long ItemId,
    string? Label);
