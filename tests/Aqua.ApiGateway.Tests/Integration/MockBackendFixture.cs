using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aqua.ApiGateway.Tests.Integration;

/// <summary>
/// In-process mock backend that the gateway forwards to. Owns its own Kestrel on a random port.
/// Tests configure per-path responses via <see cref="SetResponse"/>.
/// </summary>
public sealed class MockBackendFixture : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly ConcurrentDictionary<string, Func<HttpContext, Task>> _handlers = new();

    public string BaseUrl { get; }

    public List<(string Path, IDictionary<string, string> Headers)> ReceivedRequests { get; } = new();

    public MockBackendFixture()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddRouting();

        _app = builder.Build();

        _app.Use(async (ctx, next) =>
        {
            ReceivedRequests.Add((ctx.Request.Path.Value ?? "", ctx.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())));
            if (_handlers.TryGetValue(ctx.Request.Path.Value ?? "", out var handler))
            {
                await handler(ctx);
                return;
            }
            await next();
        });

        _app.MapGet("/{**catchall}", () => Results.Ok(new { ok = true }));
        _app.MapPost("/{**catchall}", () => Results.Ok(new { ok = true }));

        _app.Start();

        // Capture the actual bound URL.
        var serverFeature = _app.Services.GetRequiredService<IServer>()
                                .Features.Get<Microsoft.AspNetCore.Hosting.Server.Features.IServerAddressesFeature>();
        BaseUrl = serverFeature!.Addresses.First();
    }

    public void SetResponse(string path, int statusCode, string? jsonBody = null)
    {
        _handlers[path] = async ctx =>
        {
            ctx.Response.StatusCode = statusCode;
            if (jsonBody is not null)
            {
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(jsonBody);
            }
        };
    }

    public void SetDelayedFailure(string path, TimeSpan delay)
    {
        _handlers[path] = async ctx =>
        {
            await Task.Delay(delay);
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
        };
    }

    public async ValueTask DisposeAsync()
    {
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}
