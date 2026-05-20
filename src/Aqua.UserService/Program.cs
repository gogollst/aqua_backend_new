using Aqua.UserService.Infrastructure;
using Aqua.UserService.Persistence;
using NHibernate;
using ISession = NHibernate.ISession;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<ProblemDetailsFactory>();
builder.Services.AddScoped<ExceptionMappingFilter>();
builder.Services.AddControllers(opts =>
{
    opts.Filters.Add<ExceptionMappingFilter>();
});

var connStr = builder.Configuration["UserService:ConnectionString"]
    ?? throw new InvalidOperationException("Missing UserService:ConnectionString config");

builder.Services.AddSingleton<ISessionFactory>(_ =>
    new UserServiceSessionFactoryBuilder(connStr).Build());

builder.Services.AddScoped<ISession>(sp => sp.GetRequiredService<ISessionFactory>().OpenSession());

var app = builder.Build();

app.MapGet("/healthz", () => Results.Ok(new { status = "ok", service = "user-service" }));

app.MapControllers();

app.Run();

public partial class Program;
