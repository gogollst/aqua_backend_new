namespace Aqua.IdentityService.Authentication;

public enum AuthenticationFailureReason
{
    UnknownUser,
    WrongPassword,
    AccountLocked,
    AccountDeleted,
    AccountDisabled,
    PasswordExpired,
    LdapServerUnavailable,
    InternalError,
}

public sealed record AuthenticationResult
{
    public required bool Success { get; init; }
    public int? UserId { get; init; }
    public string? UserName { get; init; }
    public AuthenticationFailureReason? FailureReason { get; init; }
    public string? FailureDetail { get; init; }

    public static AuthenticationResult Succeed(int userId, string userName) =>
        new() { Success = true, UserId = userId, UserName = userName };

    public static AuthenticationResult Fail(AuthenticationFailureReason reason, string? detail = null) =>
        new() { Success = false, FailureReason = reason, FailureDetail = detail };
}
