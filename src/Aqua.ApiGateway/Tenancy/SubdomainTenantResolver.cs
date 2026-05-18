using System.Text.RegularExpressions;
using Aqua.ApiGateway.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Aqua.ApiGateway.Tenancy;

public sealed class SubdomainTenantResolver : ITenantResolver
{
    private readonly Regex? _pattern;
    private readonly HashSet<string> _reserved;

    public SubdomainTenantResolver(IOptions<TenantResolutionOptions> options)
    {
        var opts = options.Value;
        _pattern = opts.SubdomainPattern is null
            ? null
            : new Regex(opts.SubdomainPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(50));
        _reserved = new HashSet<string>(opts.ReservedSubdomains, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryResolve(HttpContext httpContext, out string? tenant)
    {
        tenant = null;
        if (_pattern is null) return false;

        var host = httpContext.Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return false;

        var match = _pattern.Match(host);
        if (!match.Success || match.Groups.Count < 2) return false;

        var candidate = match.Groups[1].Value.ToLowerInvariant();
        if (_reserved.Contains(candidate)) return false;

        tenant = candidate;
        return true;
    }
}
