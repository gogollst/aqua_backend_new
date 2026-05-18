namespace Aqua.Contracts.Pagination;

/// <summary>
/// Standard pagination parameters used in REST list-endpoints.
/// </summary>
public sealed class PageRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    public int Page
    {
        get => _page;
        init
        {
            if (value < 1) throw new ArgumentOutOfRangeException(nameof(value), "Page must be >= 1.");
            _page = value;
        }
    }

    public int PageSize
    {
        get => _pageSize;
        init
        {
            if (value < 1 || value > 500)
                throw new ArgumentOutOfRangeException(nameof(value), "PageSize must be in [1, 500].");
            _pageSize = value;
        }
    }
}
