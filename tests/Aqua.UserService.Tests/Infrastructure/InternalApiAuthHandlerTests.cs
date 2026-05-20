using Aqua.UserService.Infrastructure.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Text.Encodings.Web;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class InternalApiAuthHandlerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public InternalApiAuthHandlerTests(WebApplicationFactory<Program> factory)
        => _factory = factory;

    /// <summary>
    /// Regression for the original Stage 1-3 review finding: the DI container never populated
    /// <see cref="InternalApiAuthSchemeOptions.Options"/>, so the first request hitting
    /// <see cref="InternalApiAuthHandler.HandleAuthenticateAsync"/> would NRE on
    /// <c>Options.Options.Value</c>.  After wiring
    /// <see cref="InternalApiSchemePostConfigureOptions"/> into DI, the scheme options
    /// resolved through <see cref="IOptionsMonitor{TOptions}"/> must expose a non-null
    /// <c>Options</c> with the configured token.
    /// </summary>
    [Fact]
    public void Scheme_options_resolved_from_DI_have_Options_populated()
    {
        using var scope = _factory.Services.CreateScope();
        var monitor = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<InternalApiAuthSchemeOptions>>();

        var schemeOptions = monitor.Get(InternalApiAuthHandler.SchemeName);

        schemeOptions.Options.Should().NotBeNull("DI must populate scheme options via PostConfigure");
        schemeOptions.Options.Value.Should().NotBeNull();
        schemeOptions.Options.Value.Token.Should().NotBeNullOrEmpty(
            "configured InternalApi:Token from appsettings.json must flow into scheme options");
    }


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
