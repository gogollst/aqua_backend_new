using Aqua.ApiGateway.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Readers;

namespace Aqua.ApiGateway.OpenApi;

public sealed class OpenApiAggregationHostedService : BackgroundService
{
    private static readonly TimeSpan RefreshInterval = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan StartupRetryDelay = TimeSpan.FromSeconds(5);
    private const int StartupRetryAttempts = 3;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsMonitor<GatewayOptions> _options;
    private readonly OpenApiAggregationCache _cache;
    private readonly ILogger<OpenApiAggregationHostedService> _logger;

    public OpenApiAggregationHostedService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<GatewayOptions> options,
        OpenApiAggregationCache cache,
        ILogger<OpenApiAggregationHostedService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= StartupRetryAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            var ok = await RefreshOnceAsync(stoppingToken);
            if (ok) break;
            _logger.LogWarning("OpenAPI aggregation startup attempt {Attempt} failed; retrying in {Delay}s",
                attempt, StartupRetryDelay.TotalSeconds);
            await Task.Delay(StartupRetryDelay, stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try { await Task.Delay(RefreshInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }

            await RefreshOnceAsync(stoppingToken);
        }
    }

    internal async Task<bool> RefreshOnceAsync(CancellationToken ct)
    {
        var services = _options.CurrentValue.Services;
        var docs = new List<(string Name, Microsoft.OpenApi.Models.OpenApiDocument Doc)>();
        var allOk = true;

        foreach (var svc in services)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var url = svc.BaseUrl.TrimEnd('/') + "/openapi/v1.json";
                var response = await client.GetAsync(url, ct);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenAPI fetch from {Service} returned {Status}", svc.Name, (int)response.StatusCode);
                    allOk = false;
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                var doc = new OpenApiStreamReader().Read(stream, out var diagnostic);
                if (diagnostic.Errors.Count > 0)
                {
                    _logger.LogWarning("OpenAPI from {Service} had {Errors} parse errors — first: {First}",
                        svc.Name, diagnostic.Errors.Count, diagnostic.Errors[0].Message);
                }
                docs.Add((svc.Name, doc));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Failed to fetch OpenAPI from {Service}", svc.Name);
                allOk = false;
            }
        }

        if (docs.Count == 0)
            return false;

        var merged = OpenApiAggregator.Merge(docs);
        _cache.Set(merged);
        _logger.LogInformation("OpenAPI aggregation refreshed from {Count} services (allOk={AllOk})", docs.Count, allOk);
        return docs.Count > 0;  // partial success still updates the cache
    }
}
