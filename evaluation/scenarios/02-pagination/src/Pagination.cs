namespace Evaluation.Pagination;

public sealed record Product(int Id, string Name);
public sealed record PageResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);

public static class ProductPaging
{
    public static PageResult<Product> GetPage(IEnumerable<Product> products, int page = 1, int pageSize = 20)
    {
        if (page < 1) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var ordered = products.OrderBy(x => x.Id).ToList();
        var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return new PageResult<Product>(items, ordered.Count, page, pageSize);
    }
}
