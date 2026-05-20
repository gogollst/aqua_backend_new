using Aqua.UserService.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using NHibernate;

namespace Aqua.UserService.Infrastructure;

public sealed class ExceptionMappingFilter : IExceptionFilter
{
    private readonly ProblemDetailsFactory _factory;
    private readonly ILogger<ExceptionMappingFilter>? _log;

    public ExceptionMappingFilter(ProblemDetailsFactory factory, ILogger<ExceptionMappingFilter>? log = null)
    {
        _factory = factory;
        _log = log;
    }

    public void OnException(ExceptionContext ctx)
    {
        var correlationId = _factory.GetCorrelationId(ctx.HttpContext);
        AquaProblemDetails pd;
        int status;

        switch (ctx.Exception)
        {
            case NotFoundException nf:
                status = 404;
                pd = _factory.Build(404, nf.ErrorCode, "Not found", nf.Message, correlationId);
                break;
            case ConflictException cf:
                status = 409;
                pd = _factory.Build(409, cf.ErrorCode, "Conflict", cf.Message, correlationId);
                break;
            case ForbiddenException fb:
                status = 403;
                pd = _factory.Build(403, fb.ErrorCode, "Forbidden", fb.Message, correlationId);
                break;
            case BusinessRuleViolationException br:
                status = 422;
                pd = _factory.Build(422, br.ErrorCode, "Business rule violation", br.Message, correlationId);
                break;
            case StaleVersionException sv:
                status = 409;
                pd = _factory.Build(409, sv.ErrorCode, "Stale version", sv.Message, correlationId);
                break;
            case StaleObjectStateException nhStale:
                status = 409;
                pd = _factory.Build(409, "concurrency.stale-version", "Stale version", nhStale.Message, correlationId);
                break;
            default:
                status = 500;
                _log?.LogError(ctx.Exception, "Unhandled exception (correlationId={CorrelationId})", correlationId);
                pd = _factory.Build(500, "server.unhandled", "Internal server error",
                    "An unexpected error occurred.", correlationId);
                break;
        }

        ctx.Result = new ObjectResult(pd)
        {
            StatusCode = status,
            ContentTypes = { "application/problem+json" }
        };
        ctx.ExceptionHandled = true;
    }
}
