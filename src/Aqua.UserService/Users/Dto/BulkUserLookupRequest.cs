namespace Aqua.UserService.Users.Dto;

/// <summary>
/// Body of POST /api/v1/users/lookup — clients (e.g. the UI's user picker) pass a list
/// of ids they've collected from other places (assignees on requirements, watchers on
/// defects) and receive the matching <see cref="UserDto"/>s in one round-trip.
/// </summary>
public sealed record BulkUserLookupRequest(IReadOnlyList<long> Ids);
