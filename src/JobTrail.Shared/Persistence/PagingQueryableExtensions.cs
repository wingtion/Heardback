using JobTrail.Shared.Paging;
using Microsoft.EntityFrameworkCore;

namespace JobTrail.Shared.Persistence;

public static class PagingQueryableExtensions
{
    /// <summary>
    /// Counts and fetches one page. The query must already be ordered, or pages are not stable.
    /// </summary>
    public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(
        this IQueryable<T> query,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.PageSize).ToListAsync(cancellationToken);

        return new PagedResponse<T>(items, page.Page, page.PageSize, totalCount);
    }
}
