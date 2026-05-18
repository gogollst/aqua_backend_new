var builder = WebApplication.CreateBuilder(args);

// Health probe only — full wiring happens in Task A14
var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

// Make Program visible to WebApplicationFactory in tests.
public partial class Program;
