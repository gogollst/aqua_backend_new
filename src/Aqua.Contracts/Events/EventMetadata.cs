namespace Aqua.Contracts.Events;

/// <summary>
/// Headers propagated with every integration event: tenant, originating user, correlation id, etc.
/// </summary>
/// <remarks>
/// The named accessors (<see cref="TenantId"/>, <see cref="OriginalUserId"/>, <see cref="CorrelationId"/>)
/// treat <c>null</c> assignments as a no-op (the existing value is preserved). To clear a header,
/// remove it from <see cref="Headers"/> directly.
/// </remarks>
public sealed class EventMetadata
{
    public Dictionary<string, string> Headers { get; } = new();

    public string? TenantId
    {
        get => Headers.TryGetValue("X-Aqua-Tenant", out var v) ? v : null;
        set { if (value is not null) Headers["X-Aqua-Tenant"] = value; }
    }

    public string? OriginalUserId
    {
        get => Headers.TryGetValue("X-Aqua-Original-User", out var v) ? v : null;
        set { if (value is not null) Headers["X-Aqua-Original-User"] = value; }
    }

    public string? CorrelationId
    {
        get => Headers.TryGetValue("X-Correlation-Id", out var v) ? v : null;
        set { if (value is not null) Headers["X-Correlation-Id"] = value; }
    }
}
