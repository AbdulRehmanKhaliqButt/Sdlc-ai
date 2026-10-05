using Evaluation.Pagination;
using Xunit;

namespace Evaluation.Pagination.Tests;

public sealed class PaginationTests
{
    private static readonly Product[] Products =
    [
        new(3, "C"),
        new(1, "A"),
        new(2, "B")
    ];

    [Fact]
    public void Defaults_are_page_one_and_twenty()
    {
        var result = ProductPaging.GetPage(Products);

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal([1, 2, 3], result.Items.Select(x => x.Id));
    }

    [Fact]
    public void Empty_pages_are_valid()
    {
        var result = ProductPaging.GetPage(Products, page: 10, pageSize: 2);

        Assert.Empty(result.Items);
        Assert.Equal(3, result.TotalCount);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Invalid_paging_is_rejected(int page, int pageSize)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProductPaging.GetPage(Products, page, pageSize));
    }
}
