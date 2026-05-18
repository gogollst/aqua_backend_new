namespace Aqua.Contracts;

/// <summary>
/// Strongly-typed tenant identifier. Always lowercase, non-empty, slug-format.
/// </summary>
public readonly record struct TenantId
{
    public string Value { get; }

    public TenantId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TenantId must not be empty.", nameof(value));
        Value = value;
    }

    public static implicit operator string(TenantId id) => id.Value;
    public override string ToString() => Value;
}
