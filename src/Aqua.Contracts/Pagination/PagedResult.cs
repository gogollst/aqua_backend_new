namespace Aqua.Contracts.Pagination;

/// <summary>
/// Paginated list response. Used as return type for REST list-endpoints.
/// </summary>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public long TotalCount { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    public PagedResult(IReadOnlyList<T> items, long totalCount, int page, int pageSize)
    {
        Items = items ?? throw new ArgumentNullException(nameof(items));
        if (totalCount < 0) throw new ArgumentOutOfRangeException(nameof(totalCount), "TotalCount must be >= 0.");
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page), "Page must be >= 1.");
        if (pageSize < 1) throw new ArgumentOutOfRangeException(nameof(pageSize), "PageSize must be >= 1.");
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
    }
}
