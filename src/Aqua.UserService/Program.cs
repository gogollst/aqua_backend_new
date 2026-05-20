using Aqua.Data.Outbox;
using Aqua.UserService.Bookmarks;
using Aqua.UserService.ClaimsLookup;
using Aqua.UserService.Events;
using Aqua.UserService.Infrastructure;
using Aqua.UserService.Infrastructure.Authorization;
using Aqua.UserService.Ldap;
using Aqua.UserService.Persistence;
using Aqua.UserService.Profiles;
using Aqua.UserService.Roles;
using Aqua.UserService.Tenants;
using Aqua.UserService.Users;
using Aqua.UserService.Views;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using NHibernate;
using ISession = NHibernate.ISession;

var builder = WebApplication.CreateBuilder(args);

var publicPort   = builder.Configuration.GetValue<int>("UserService:PublicPort", 8080);
var internalPort = builder.Configuration.GetValue<int>("UserService:InternalPort", 8081);

builder.WebHost.ConfigureKestrel(opts =>
{
    opts.ListenAnyIP(publicPort);
    opts.ListenAnyIP(internalPort, lo =>
    {
        var internalOpts = builder.Configuration.GetSection("InternalApi").Get<InternalApiAuthOptions>();
        if (internalOpts?.RequireMtls == true)
        {
            lo.UseHttps(httpsOpts =>
            {
                httpsOpts.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
            });
        }
    });
});

// --- DI registrations ---
var connStr = builder.Configuration["UserService:ConnectionString"]
    ?? throw new InvalidOperationException("Missing UserService:ConnectionString config");

builder.Services.AddSingleton<ISessionFactory>(_ =>
    new UserServiceSessionFactoryBuilder(connStr).Build());

builder.Services.AddScoped<CurrentTenant>();
builder.Services.AddScoped<ICurrentTenant>(sp => sp.GetRequiredService<CurrentTenant>());
builder.Services.AddScoped<ISession>(sp =>
{
    var factory = sp.GetRequiredService<ISessionFactory>();
    var session = factory.OpenSession();
    var tenant = sp.GetRequiredService<ICurrentTenant>();
    if (tenant.IsResolved && tenant.Id.HasValue)
    {
        TenantFilter.EnableFor(session, tenant.Id.Value);
    }
    return session;
});

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserManager, UserManager>();

builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IRoleManager, RoleManager>();

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<ICustomerUserAssignmentRepository, CustomerUserAssignmentRepository>();
builder.Services.AddScoped<ITenantManager, TenantManager>();
builder.Services.AddScoped<ITenantBootstrapper, TenantBootstrapper>();

builder.Services.AddScoped<ILdapGroupRoleMappingRepository, LdapGroupRoleMappingRepository>();
builder.Services.AddScoped<ILdapGroupMappingManager, LdapGroupMappingManager>();
builder.Services.AddScoped<ILdapJitSyncer, LdapJitSyncer>();

builder.Services.AddScoped<IUserViewRepository, UserViewRepository>();
builder.Services.AddScoped<IUserViewManager, UserViewManager>();

builder.Services.AddScoped<IBookmarkRepository, BookmarkRepository>();
builder.Services.AddScoped<IBookmarkManager, BookmarkManager>();

builder.Services.AddScoped<IProfileManager, ProfileManager>();
builder.Services.AddScoped<IClaimsLookupService, ClaimsLookupService>();

// --- Outbox + event publishing ---
// UserService persists outbox rows via its own NHibernate session, so we bind IOutboxWriter to
// the UserService-local implementation rather than the Aqua.Data default (which expects an
// ISessionScope abstraction we don't register here).
builder.Services.AddScoped<IOutboxWriter, UserServiceOutboxWriter>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserEventPublisher>(sp =>
{
    var outbox = sp.GetRequiredService<IOutboxWriter>();
    var http   = sp.GetRequiredService<IHttpContextAccessor>();

    string CorrelationId()
    {
        // Per the architecture decision, correlation propagation rides on the HTTP
        // X-Correlation-Id header; fall back to TraceIdentifier when the request didn't supply one
        // and to a fresh GUID for non-HTTP origin (e.g. hosted services).
        var ctx = http.HttpContext;
        if (ctx is null) return Guid.NewGuid().ToString("N");
        if (ctx.Request.Headers.TryGetValue("X-Correlation-Id", out var hdr) &&
            !string.IsNullOrWhiteSpace(hdr.ToString()))
            return hdr.ToString();
        return ctx.TraceIdentifier;
    }

    string CausationId()
    {
        // Causation defaults to the inbound request id; downstream services can override by
        // setting X-Causation-Id.
        var ctx = http.HttpContext;
        if (ctx is null) return Guid.NewGuid().ToString("N");
        if (ctx.Request.Headers.TryGetValue("X-Causation-Id", out var hdr) &&
            !string.IsNullOrWhiteSpace(hdr.ToString()))
            return hdr.ToString();
        return ctx.TraceIdentifier;
    }

    Actor ActorProvider()
    {
        var ctx = http.HttpContext;
        var sub = ctx?.User?.FindFirst("sub")?.Value
               ?? ctx?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (long.TryParse(sub, System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out var uid))
            return new Actor("user", uid);
        return new Actor("system");
    }

    return new UserEventPublisher(outbox, CorrelationId, CausationId, ActorProvider);
});

builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, PermissionAuthorizationHandler>();
builder.Services.Configure<InternalApiAuthOptions>(builder.Configuration.GetSection("InternalApi"));
// AddScheme<TOptions, THandler>(..., _ => {}) leaves InternalApiAuthSchemeOptions.Options null.
// PostConfigure copies the strongly-typed InternalApiAuthOptions into the scheme options so the
// handler can read it without NRE'ing on Options.Options.Value.
builder.Services.AddSingleton<Microsoft.Extensions.Options.IPostConfigureOptions<InternalApiAuthSchemeOptions>,
    InternalApiSchemePostConfigureOptions>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opts =>
    {
        // Fallback URL keeps dev/test configs working when IdentityService:Authority is unset;
        // appsettings.Production.json must override with the real issuer.
        opts.Authority = builder.Configuration["IdentityService:Authority"] ?? "http://localhost:5001";
        opts.Audience  = "aqua-user-service";
        opts.RequireHttpsMetadata = builder.Environment.IsProduction();
    })
    .AddScheme<InternalApiAuthSchemeOptions, InternalApiAuthHandler>(InternalApiAuthHandler.SchemeName, _ => { });

builder.Services.AddAuthorizationBuilder();
// PermissionPolicyProvider resolves `perm:{long}` policies dynamically, so we never have
// to enumerate the 24 permissions explicitly via AddPolicy().
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddControllers(opts => opts.Filters.Add<ExceptionMappingFilter>());

var app = builder.Build();

// Public listener path-filter — exclude /internal/v1/* from public port.
app.UseWhen(ctx => ctx.Connection.LocalPort == publicPort, branch =>
{
    branch.UseMiddleware<TenantContextMiddleware>();
    branch.UseAuthentication();
    branch.UseAuthorization();
});

// Internal listener — only /internal/v1/* allowed; use InternalApi scheme exclusively.
app.UseWhen(ctx => ctx.Connection.LocalPort == internalPort, branch =>
{
    branch.Use(async (ctx, next) =>
    {
        if (!ctx.Request.Path.StartsWithSegments("/internal/v1"))
        {
            ctx.Response.StatusCode = 404;
            return;
        }
        await next();
    });
    branch.UseAuthentication();
    branch.UseAuthorization();
});

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "user-service" }))
   .AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
