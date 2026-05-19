using System.Threading.RateLimiting;
using Aqua.ApiGateway.Authentication;
using Aqua.ApiGateway.Configuration;
using Aqua.ApiGateway.Headers;
using Aqua.ApiGateway.HealthChecks;
using Aqua.ApiGateway.Observability;
using Aqua.ApiGateway.OpenApi;
using Aqua.ApiGateway.RateLimiting;
using Aqua.ApiGateway.Resilience;
using Aqua.ApiGateway.Routing;
using Aqua.ApiGateway.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ---- Serilog (configure earliest possible) ----
builder.Host.UseSerilog((ctx, lc) => lc
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("service", "api-gateway")
    .WriteTo.Console(new Serilog.Formatting.Compact.CompactJsonFormatter()));

// ---- Options binding ----
builder.Services.AddOptions<GatewayOptions>()
    .Bind(builder.Configuration.GetSection("Gateway"))
    .ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<TenantResolutionOptions>()
    .Bind(builder.Configuration.GetSection("TenantResolution"))
    .Validate(o =>
    {
        var ctx = new System.ComponentModel.DataAnnotations.ValidationContext(o);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(o, ctx, results, true);
    }, "TenantResolutionOptions invalid")
    .ValidateOnStart();
builder.Services.AddOptions<RateLimitOptions>().Bind(builder.Configuration.GetSection("RateLimits"));
builder.Services.AddOptions<ResilienceOptions>().Bind(builder.Configuration.GetSection("Resilience"));

// ---- HttpClient + Polly v8 resilience for YARP ----
builder.Services.AddHttpClient();
builder.Services.AddProxyResilience();

// ---- Observability ----
builder.Services.AddSingleton<GatewayMeter>();
builder.Services.AddOpenTelemetry()
    .WithMetrics(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter(GatewayMeter.MeterName))
    .WithTracing(b => b
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// ---- Authentication (JwtBearer with built-in JWKS via OIDC discovery) ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var gw = builder.Configuration.GetSection("Gateway").Get<GatewayOptions>()!;
        options.Authority = gw.JwtAuthority;
        options.Audience = gw.JwtAudience;
        options.RequireHttpsMetadata = gw.JwtRequireHttpsMetadata;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromSeconds(5),
        };
        // ConfigurationManager is created automatically by the handler from Authority;
        // override the refresh cadence so we re-fetch the JWKS every 10 min,
        // and on key-not-found at most every 30 seconds (the default is 30s minimum).
        options.RefreshOnIssuerKeyNotFound = true;
        options.AutomaticRefreshInterval = TimeSpan.FromMinutes(10);
        options.RefreshInterval = TimeSpan.FromSeconds(30);
    });

builder.Services.AddAuthorization(opts =>
{
    opts.AddPolicy(YarpClusters.AnonymousPolicy, p => p.RequireAssertion(_ => true));
    opts.AddPolicy(YarpClusters.RequireAuthenticatedPolicy, p => p.RequireAuthenticatedUser());
});

// ---- Tenancy ----
builder.Services.AddSingleton<ITenantResolver, SubdomainTenantResolver>();
builder.Services.AddSingleton<ITenantResolver, HeaderTenantResolver>();
builder.Services.AddSingleton<ITenantResolver, DefaultTenantResolver>();

// ---- Rate limiter (post-auth: per-tenant + per-user) ----
builder.Services.AddRateLimiter(rl =>
{
    rl.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    rl.OnRejected = async (ctx, ct) =>
    {
        await PerIpRateLimitMiddleware.WriteRateLimitedAsync(ctx.HttpContext, 60, "per-tenant-or-per-user");
    };

    var rlOpts = builder.Configuration.GetSection("RateLimits").Get<RateLimitOptions>()!;

    rl.AddPolicy(PartitionedRateLimitPolicies.PerTenantPolicyName,
        ctx => PartitionedRateLimitPolicies.PerTenant(ctx, rlOpts.PerTenant.PermitLimit, rlOpts.PerTenant.WindowSeconds));
    rl.AddPolicy(PartitionedRateLimitPolicies.PerUserPolicyName,
        ctx => PartitionedRateLimitPolicies.PerUser(ctx, rlOpts.PerUser.PermitLimit, rlOpts.PerUser.WindowSeconds));

    rl.GlobalLimiter = PartitionedRateLimiter.CreateChained(
        PartitionedRateLimiter.Create<HttpContext, string>(c =>
            PartitionedRateLimitPolicies.PerTenant(c, rlOpts.PerTenant.PermitLimit, rlOpts.PerTenant.WindowSeconds)),
        PartitionedRateLimiter.Create<HttpContext, string>(c =>
            PartitionedRateLimitPolicies.PerUser(c, rlOpts.PerUser.PermitLimit, rlOpts.PerUser.WindowSeconds))
    );
});

// ---- OpenAPI aggregation ----
builder.Services.AddSingleton<OpenApiAggregationCache>();
builder.Services.AddHostedService<OpenApiAggregationHostedService>();

// ---- Health checks ----
builder.Services.AddHttpClient<JwksReachabilityHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<JwksReachabilityHealthCheck>(JwksReachabilityHealthCheck.Name, tags: new[] { "ready" });

// ---- YARP ----
var gatewayOptions = builder.Configuration.GetSection("Gateway").Get<GatewayOptions>()!;
var (routes, clusters) = YarpClusters.Build(gatewayOptions);
builder.Services.AddReverseProxy().LoadFromMemory(routes, clusters);

var app = builder.Build();

// ---- Pipeline ----
app.UseSerilogRequestLogging();
app.UseForwardedHeaders(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
                       | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedHost,
});

if (!app.Environment.IsDevelopment())
    app.UseHttpsRedirection();

app.UseMiddleware<PerIpRateLimitMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<TenantClaimValidator>();
app.UseRateLimiter();
app.UseMiddleware<HeaderWhitelistMiddleware>();
app.UseMiddleware<HeaderEnrichmentMiddleware>();
app.UseAuthorization();

// Gateway-internal routes
app.MapGet("/healthz", () => Results.Ok(new { status = "ok" })).AllowAnonymous();
app.MapHealthChecks("/readyz", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = h => h.Tags.Contains("ready"),
}).AllowAnonymous();
app.MapAggregatedOpenApi();

// YARP reverse proxy
app.MapReverseProxy();

app.Run();

public partial class Program;
