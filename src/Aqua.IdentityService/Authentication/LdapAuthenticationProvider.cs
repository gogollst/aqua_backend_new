using System.DirectoryServices.Protocols;
using System.Net;
using Aqua.IdentityService.Configuration;
using Aqua.IdentityService.Domain;
using Microsoft.Extensions.Options;

namespace Aqua.IdentityService.Authentication;

public sealed class LdapAuthenticationProvider : IAuthenticationProvider
{
    private readonly LdapOptions _options;
    private readonly IUserRepository _users;

    public LdapAuthenticationProvider(IOptions<LdapOptions> options, IUserRepository users)
    {
        _options = options.Value;
        _users = users;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string userName, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
            return AuthenticationResult.Fail(AuthenticationFailureReason.WrongPassword);

        // Map LDAP username → aqua user row (must exist in aquaUser table to be allowed in).
        var user = await _users.FindByUserNameAsync(userName, ct);
        if (user is null) return AuthenticationResult.Fail(AuthenticationFailureReason.UnknownUser);
        if (user.Deleted) return AuthenticationResult.Fail(AuthenticationFailureReason.AccountDeleted);

        try
        {
            using var connection = new LdapConnection(new LdapDirectoryIdentifier(_options.Host, _options.Port));
            connection.SessionOptions.SecureSocketLayer = _options.UseSsl;
            connection.AuthType = AuthType.Basic;

            // Attempt bind with user-supplied credentials. Success ⇒ password is valid.
            var userDn = $"{_options.UserNameAttribute}={userName},{_options.BaseDn}";
            connection.Bind(new NetworkCredential(userDn, password));

            return AuthenticationResult.Succeed(user.Id, user.UserName);
        }
        catch (LdapException ex) when (ex.ErrorCode == 49) // invalidCredentials
        {
            return AuthenticationResult.Fail(AuthenticationFailureReason.WrongPassword);
        }
        catch (LdapException ex)
        {
            return AuthenticationResult.Fail(AuthenticationFailureReason.LdapServerUnavailable, ex.Message);
        }
        catch (Exception ex)
        {
            return AuthenticationResult.Fail(AuthenticationFailureReason.InternalError, ex.Message);
        }
    }
}
