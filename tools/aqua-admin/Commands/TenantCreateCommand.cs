using System.CommandLine;
using System.Net.Http.Json;

namespace Aqua.Admin.Cli.Commands;

public sealed class TenantCreateCommand
{
    public Command Build()
    {
        var cmd = new Command("tenant", "Tenant operations");
        var create = new Command("create", "Bootstrap a new tenant");

        var slugOpt     = new Option<string>("--slug") { IsRequired = true };
        var nameOpt     = new Option<string>("--display-name") { IsRequired = true };
        var modeOpt     = new Option<string>("--auth-mode", () => "local");
        var adminOpt    = new Option<string>("--admin-username", () => "admin");
        var emailOpt    = new Option<string>("--admin-email") { IsRequired = true };
        var urlOpt      = new Option<string>("--user-service-internal-url", () => "http://localhost:5003");
        var tokenOpt    = new Option<string>("--internal-token") { IsRequired = true };
        var defaultsOpt = new Option<string>("--default-roles", () => "standard");

        create.AddOption(slugOpt);
        create.AddOption(nameOpt);
        create.AddOption(modeOpt);
        create.AddOption(adminOpt);
        create.AddOption(emailOpt);
        create.AddOption(urlOpt);
        create.AddOption(tokenOpt);
        create.AddOption(defaultsOpt);

        create.SetHandler(async (string slug, string display, string mode, string admin,
                                  string email, string url, string token, string defaults) =>
        {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            client.DefaultRequestHeaders.Add("X-Internal-Token", token);

            var body = new
            {
                slug,
                displayName = display,
                primaryDomain = (string?)null,
                auth = new { mode, ldap = (object?)null, saml = (object?)null },
                adminUser = new
                {
                    username = admin,
                    email,
                    firstName = "Initial",
                    surname = "Admin",
                    passwordMode = "generate",
                    password = (string?)null
                },
                defaultRoles = defaults
            };
            var resp = await client.PostAsJsonAsync("/internal/v1/tenants/bootstrap", body);
            var content = await resp.Content.ReadAsStringAsync();
            Console.WriteLine($"Status: {(int)resp.StatusCode}");
            Console.WriteLine(content);
            Environment.Exit(resp.IsSuccessStatusCode ? 0 : 1);
        }, slugOpt, nameOpt, modeOpt, adminOpt, emailOpt, urlOpt, tokenOpt, defaultsOpt);

        cmd.AddCommand(create);
        return cmd;
    }
}
