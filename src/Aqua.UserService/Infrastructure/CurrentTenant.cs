namespace Aqua.UserService.Infrastructure;

public interface ICurrentTenant
{
    string? Slug { get; }
    long?   Id   { get; }
    bool    IsResolved { get; }
    void    Set(string slug, long? id = null);
}

public sealed class CurrentTenant : ICurrentTenant
{
    public string? Slug { get; private set; }
    public long?   Id   { get; private set; }
    public bool    IsResolved => Slug is not null;

    public void Set(string slug, long? id = null)
    {
        Slug = slug;
        Id = id;
    }
}
