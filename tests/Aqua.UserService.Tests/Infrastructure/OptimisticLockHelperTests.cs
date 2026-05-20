using Aqua.UserService.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class OptimisticLockHelperTests
{
    [Fact]
    public void Resolves_version_from_If_Match_header()
    {
        var http = new DefaultHttpContext();
        http.Request.Headers[HeaderNames.IfMatch] = "\"42\"";
        OptimisticLockHelper.ResolveVersion(http, bodyVersion: 100).Should().Be(42);
    }

    [Fact]
    public void Falls_back_to_body_when_header_absent()
    {
        var http = new DefaultHttpContext();
        OptimisticLockHelper.ResolveVersion(http, bodyVersion: 100).Should().Be(100);
    }

    [Fact]
    public void Throws_when_neither_present()
    {
        var http = new DefaultHttpContext();
        var act = () => OptimisticLockHelper.ResolveVersion(http, bodyVersion: null);
        act.Should().Throw<InvalidOperationException>();
    }
}
