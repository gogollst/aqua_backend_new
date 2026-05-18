using Aqua.Data.DependencyInjection;
using Aqua.Data.Tenancy;
using Aqua.IdentityService.Authentication;
using Aqua.IdentityService.Configuration;
using Aqua.IdentityService.Domain;
using Aqua.IdentityService.Endpoints;
using Aqua.IdentityService.Lockout;
using Aqua.IdentityService.PasswordExpiry;
using Aqua.IdentityService.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Options binding (with validation on start)
builder.Services.AddOptions<IdentityOptions>().Bind(builder.Configuration.GetSection("Identity")).ValidateDataAnnotations().ValidateOnStart();
builder.Services.AddOptions<LdapOptions>().Bind(builder.Configuration.GetSection("Ldap")).ValidateDataAnnotations();
builder.Services.AddOptions<AuthenticationOptions>().Bind(builder.Configuration.GetSection("Authentication"));

// NHibernate via Aqua.Data
builder.Services.AddAquaData(opts =>
{
    var section = builder.Configuration.GetSection("AquaData:Tenants");
    opts.ResolveTenantConfig = tenant =>
    {
        var t = section.GetSection(tenant.Value);
        return new TenantDbConfig(
            Enum.Parse<SupportedDbms>(t["Dbms"]!),
            t["ConnectionString"]!);
    };
}, cfg =>
{
    AquaUserMapping.Apply(cfg);
    AquaUserPasswordMapping.Apply(cfg);
    RefreshTokenMapping.Apply(cfg);
});

// Identity-service own services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<DatabaseAuthenticationProvider>();
builder.Services.AddScoped<LdapAuthenticationProvider>();
builder.Services.AddScoped<IAuthenticationProvider, CompositeAuthenticationProvider>();
builder.Services.AddSingleton<IAccountLockoutService, AccountLockoutService>();
builder.Services.AddSingleton<IPasswordExpiryService, PasswordExpiryService>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
builder.Services.AddScoped<IRefreshTokenStore, RefreshTokenStore>();
builder.Services.AddSingleton<JwksProvider>();

var app = builder.Build();

// Tenant middleware (from Aqua.Data)
app.UseMiddleware<TenantMiddleware>();

// Routes
app.MapPost("/api/v1/auth/token", TokenEndpoint.HandleAsync);
app.MapPost("/api/v1/auth/refresh", RefreshEndpoint.HandleAsync);
app.MapGet("/.well-known/openid-configuration", DiscoveryEndpoint.Handle);
app.MapGet("/.well-known/jwks.json", JwksEndpoint.Handle);

// Health probe
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Make Program visible to WebApplicationFactory in tests.
public partial class Program;
