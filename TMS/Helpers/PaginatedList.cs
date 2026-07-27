using Microsoft.EntityFrameworkCore;

namespace TMS.Helpers;

public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int Page { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages { get; }
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PaginatedList(List<T> items, int totalCount, int page, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        Page = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int page, int pageSize)
    {
        int count = await source.CountAsync();
        List<T> items = await source.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, page, pageSize);
    }
}
