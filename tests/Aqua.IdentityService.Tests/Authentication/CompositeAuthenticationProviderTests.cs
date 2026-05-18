using Aqua.IdentityService.Authentication;
using Aqua.IdentityService.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aqua.IdentityService.Tests.Authentication;

public class CompositeAuthenticationProviderTests
{
    private sealed class StaticProvider : IAuthenticationProvider
    {
        private readonly AuthenticationResult _result;
        public StaticProvider(AuthenticationResult r) { _result = r; }
        public Task<AuthenticationResult> AuthenticateAsync(string un, string pw, CancellationToken ct = default) =>
            Task.FromResult(_result);
    }

    [Fact]
    public async Task DatabaseOnlyMode_ReturnsDbResult()
    {
        var opts = Options.Create(new AuthenticationOptions { Mode = AuthenticationMode.DatabaseOnly });
        var db = new StaticProvider(AuthenticationResult.Succeed(1, "alice"));
        var ldap = new StaticProvider(AuthenticationResult.Fail(AuthenticationFailureReason.UnknownUser));
        var sut = new CompositeAuthenticationProvider(opts, db, ldap);
        var result = await sut.AuthenticateAsync("alice", "x");
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(1);
    }

    [Fact]
    public async Task BothPreferDatabase_FallsBackToLdapOnDbFailure()
    {
        var opts = Options.Create(new AuthenticationOptions { Mode = AuthenticationMode.BothPreferDatabase });
        var db = new StaticProvider(AuthenticationResult.Fail(AuthenticationFailureReason.UnknownUser));
        var ldap = new StaticProvider(AuthenticationResult.Succeed(99, "bob"));
        var sut = new CompositeAuthenticationProvider(opts, db, ldap);
        var result = await sut.AuthenticateAsync("bob", "x");
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(99);
    }

    [Fact]
    public async Task LdapOnlyUsernames_RoutesSpecificUserToLdap()
    {
        var opts = Options.Create(new AuthenticationOptions
        {
            Mode = AuthenticationMode.DatabaseOnly,
            LdapOnlyUsernames = new[] { "admin-from-ad" },
        });
        var db = new StaticProvider(AuthenticationResult.Fail(AuthenticationFailureReason.UnknownUser, "db not called"));
        var ldap = new StaticProvider(AuthenticationResult.Succeed(7, "admin-from-ad"));
        var sut = new CompositeAuthenticationProvider(opts, db, ldap);
        var result = await sut.AuthenticateAsync("admin-from-ad", "x");
        result.Success.Should().BeTrue();
        result.UserId.Should().Be(7);
    }
}
