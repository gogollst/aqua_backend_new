using Aqua.UserService.Infrastructure.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Encodings.Web;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class InternalApiAuthHandlerTests
{
    [Fact]
    public async Task Accepts_matching_internal_token()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Internal-Token"] = "good-token";
        var handler = await BuildHandler(http, configuredToken: "good-token");
        var result = await handler.AuthenticateAsync();
        result.Succeeded.Should().BeTrue();
        result.Principal!.HasClaim("internal-api", "true").Should().BeTrue();
    }

    [Fact]
    public async Task Rejects_missing_token()
    {
        var http = new DefaultHttpContext();
        var handler = await BuildHandler(http, configuredToken: "good-token");
        var result = await handler.AuthenticateAsync();
        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Rejects_wrong_token()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers["X-Internal-Token"] = "bad-token";
        var handler = await BuildHandler(http, configuredToken: "good-token");
        var result = await handler.AuthenticateAsync();
        result.Succeeded.Should().BeFalse();
    }

    private static async Task<InternalApiAuthHandler> BuildHandler(HttpContext http, string configuredToken)
    {
        var opts = Options.Create(new InternalApiAuthOptions { Token = configuredToken, RequireMtls = false });
        var optionsMonitor = Substitute.For<IOptionsMonitor<InternalApiAuthSchemeOptions>>();
        optionsMonitor.Get(InternalApiAuthHandler.SchemeName)
            .Returns(new InternalApiAuthSchemeOptions { Options = opts });
        var handler = new InternalApiAuthHandler(optionsMonitor,
            NullLoggerFactory.Instance, UrlEncoder.Default);
        await handler.InitializeAsync(new AuthenticationScheme(InternalApiAuthHandler.SchemeName,
            "InternalApi", typeof(InternalApiAuthHandler)), http);
        return handler;
    }
}
