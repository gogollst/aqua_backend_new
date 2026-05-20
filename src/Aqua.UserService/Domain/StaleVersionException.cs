namespace Aqua.UserService.Domain;

public sealed class StaleVersionException : DomainException
{
    public long CurrentVersion { get; }

    public StaleVersionException(long currentVersion, string message)
        : base("concurrency.stale-version", message)
    {
        CurrentVersion = currentVersion;
    }

    // Test/factory ctor matching the (errorCode, message) DomainException shape.
    // The errorCode argument is accepted for uniformity with other DomainException subtypes
    // but is always normalized to the canonical "concurrency.stale-version".
    public StaleVersionException(string errorCode, string message)
        : base("concurrency.stale-version", message)
    {
        _ = errorCode;
        CurrentVersion = 0L;
    }
}
