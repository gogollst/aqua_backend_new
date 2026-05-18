using Aqua.IdentityService.Domain;

namespace Aqua.IdentityService.Authentication;

public sealed class DatabaseAuthenticationProvider : IAuthenticationProvider
{
    private readonly IUserRepository _users;
    public DatabaseAuthenticationProvider(IUserRepository users) => _users = users;

    public async Task<AuthenticationResult> AuthenticateAsync(string userName, string password, CancellationToken ct = default)
    {
        var user = await _users.FindByUserNameAsync(userName, ct);
        if (user is null) return AuthenticationResult.Fail(AuthenticationFailureReason.UnknownUser);
        if (user.Deleted) return AuthenticationResult.Fail(AuthenticationFailureReason.AccountDeleted);
        if (user.UserStatus != 0) return AuthenticationResult.Fail(AuthenticationFailureReason.AccountDisabled);

        var pwd = await _users.GetPasswordForAsync(user.Id, ct);
        if (pwd is null || string.IsNullOrEmpty(pwd.Password))
            return AuthenticationResult.Fail(AuthenticationFailureReason.WrongPassword);

        if (pwd.LockedUntil is { } until && until > DateTime.UtcNow)
            return AuthenticationResult.Fail(AuthenticationFailureReason.AccountLocked);

        if (!BCrypt.Net.BCrypt.Verify(password, pwd.Password))
        {
            await _users.IncrementFailedLoginAsync(user.Id, ct);
            return AuthenticationResult.Fail(AuthenticationFailureReason.WrongPassword);
        }

        await _users.ResetFailedLoginAsync(user.Id, ct);
        return AuthenticationResult.Succeed(user.Id, user.UserName);
    }
}
