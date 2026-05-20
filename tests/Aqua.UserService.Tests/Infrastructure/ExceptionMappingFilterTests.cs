using Aqua.UserService.Domain;
using Aqua.UserService.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using NHibernate;
using NSubstitute;
using Xunit;

namespace Aqua.UserService.Tests.Infrastructure;

public sealed class ExceptionMappingFilterTests
{
    private readonly ExceptionMappingFilter _filter = new(new ProblemDetailsFactory());

    [Theory]
    [InlineData(typeof(NotFoundException), 404, "user.not-found")]
    [InlineData(typeof(ConflictException), 409, "slug.taken")]
    [InlineData(typeof(ForbiddenException), 403, "auth.forbidden")]
    [InlineData(typeof(BusinessRuleViolationException), 422, "role.permission-conflict")]
    [InlineData(typeof(StaleVersionException), 409, "concurrency.stale-version")]
    public void Maps_domain_exceptions_to_problem_details(Type exType, int expectedStatus, string errorCode)
    {
        var ex = (DomainException)Activator.CreateInstance(exType, errorCode, "test message")!;
        var ctx = MakeContext(ex);

        _filter.OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
        var objectResult = ctx.Result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(expectedStatus);
        var pd = objectResult.Value.Should().BeOfType<AquaProblemDetails>().Subject;
        pd.ErrorCode.Should().Be(errorCode);
        pd.Status.Should().Be(expectedStatus);
    }

    [Fact]
    public void Wraps_nhibernate_stale_object_as_409()
    {
        var ex = new StaleObjectStateException("Role", 17L);
        var ctx = MakeContext(ex);

        _filter.OnException(ctx);

        ctx.ExceptionHandled.Should().BeTrue();
        var objectResult = (ObjectResult)ctx.Result!;
        objectResult.StatusCode.Should().Be(409);
        var pd = (AquaProblemDetails)objectResult.Value!;
        pd.ErrorCode.Should().Be("concurrency.stale-version");
    }

    [Fact]
    public void Maps_unhandled_to_500_with_opaque_message()
    {
        var ex = new InvalidOperationException("boom — secret stack");
        var ctx = MakeContext(ex);

        _filter.OnException(ctx);

        var objectResult = (ObjectResult)ctx.Result!;
        objectResult.StatusCode.Should().Be(500);
        var pd = (AquaProblemDetails)objectResult.Value!;
        pd.Detail.Should().NotContain("secret stack");
        pd.ErrorCode.Should().Be("server.unhandled");
    }

    private static ExceptionContext MakeContext(Exception ex)
    {
        var http = new DefaultHttpContext { TraceIdentifier = "trace-123" };
        var actionCtx = new ActionContext(http, new RouteData(), new ActionDescriptor(), new ModelStateDictionary());
        return new ExceptionContext(actionCtx, new List<IFilterMetadata>()) { Exception = ex };
    }
}
