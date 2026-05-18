namespace Aqua.IdentityService.Tokens;

public sealed record TokenPair(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

public interface ITokenIssuer
{
    TokenPair Issue(int userId, string userName, string tenantId, IReadOnlyList<string> roles);
    string IssueRefreshToken(int userId, string tenantId);
}
