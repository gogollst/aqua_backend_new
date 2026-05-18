namespace Aqua.Contracts.Dto;

/// <summary>
/// Compact error envelope used when full Problem+JSON is overkill (e.g. client-side validation echoes).
/// </summary>
public sealed record ErrorResponse(string Code, string Message) : IDto;
