namespace Aqua.IdentityService.Authentication;

public interface IAuthenticationProvider
{
    Task<AuthenticationResult> AuthenticateAsync(string userName, string password, CancellationToken ct = default);
}
