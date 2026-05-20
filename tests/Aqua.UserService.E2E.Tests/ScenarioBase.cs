namespace Aqua.UserService.E2E.Tests;

/// <summary>
/// Shared plumbing for E2E scenarios. All tests in this assembly are tagged
/// <c>[Trait("Category", "E2E")]</c> and skipped by default — they require
/// <c>docker compose -f deploy/compose.e2e.yml up -d</c> to be running.
/// </summary>
public abstract class ScenarioBase
{
    protected const string GatewayBaseUrl      = "http://localhost:8080";
    protected const string UserServiceInternal = "http://localhost:5003";
    protected static string InternalToken      = "e2e-internal-token";

    protected HttpClient Gateway() => new() { BaseAddress = new Uri(GatewayBaseUrl) };

    protected HttpClient Internal()
    {
        var c = new HttpClient { BaseAddress = new Uri(UserServiceInternal) };
        c.DefaultRequestHeaders.Add("X-Internal-Token", InternalToken);
        return c;
    }
}
