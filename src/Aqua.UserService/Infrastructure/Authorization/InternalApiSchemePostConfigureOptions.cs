using Microsoft.Extensions.Options;

namespace Aqua.UserService.Infrastructure.Authorization;

/// <summary>
/// Bridges the strongly-typed <see cref="InternalApiAuthOptions"/> configured via
/// <c>builder.Services.Configure&lt;InternalApiAuthOptions&gt;(...)</c> into the
/// <see cref="InternalApiAuthSchemeOptions"/> instance the authentication framework hands to
/// <see cref="InternalApiAuthHandler"/>.  Without this, <c>Options.Options</c> on the scheme options
/// is never populated and the handler dereferences null at runtime.
/// </summary>
internal sealed class InternalApiSchemePostConfigureOptions
    : IPostConfigureOptions<InternalApiAuthSchemeOptions>
{
    private readonly IOptions<InternalApiAuthOptions> _options;

    public InternalApiSchemePostConfigureOptions(IOptions<InternalApiAuthOptions> options)
        => _options = options;

    public void PostConfigure(string? name, InternalApiAuthSchemeOptions options)
        => options.Options = _options;
}
