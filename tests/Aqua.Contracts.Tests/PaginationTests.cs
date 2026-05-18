using Aqua.Contracts.Pagination;
using FluentAssertions;
using Xunit;

namespace Aqua.Contracts.Tests;

public class PaginationTests
{
    [Fact]
    public void PageRequest_DefaultPage_IsOne()
    {
        new PageRequest().Page.Should().Be(1);
    }

    [Fact]
    public void PageRequest_DefaultPageSize_IsTwenty()
    {
        new PageRequest().PageSize.Should().Be(20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PageRequest_InvalidPage_Throws(int page)
    {
        var act = () => new PageRequest { Page = page };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PageRequest_PageSizeAbove500_Throws()
    {
        var act = () => new PageRequest { PageSize = 501 };
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void PagedResult_Construction_StoresFields()
    {
        var result = new PagedResult<string>(new[] { "a", "b" }, totalCount: 42, page: 2, pageSize: 20);
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(42);
        result.Page.Should().Be(2);
        result.PageSize.Should().Be(20);
        result.TotalPages.Should().Be(3);
    }
}
