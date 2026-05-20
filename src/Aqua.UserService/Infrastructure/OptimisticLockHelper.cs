using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Aqua.UserService.Infrastructure;

public static class OptimisticLockHelper
{
    public static long ResolveVersion(HttpContext http, long? bodyVersion)
    {
        if (http.Request.Headers.TryGetValue(HeaderNames.IfMatch, out var value))
        {
            var raw = value.ToString().Trim('"');
            if (long.TryParse(raw, out var v)) return v;
            throw new InvalidOperationException($"Malformed If-Match header: {value}");
        }
        if (bodyVersion.HasValue) return bodyVersion.Value;
        throw new InvalidOperationException("No version supplied (neither If-Match header nor body.version).");
    }
}
