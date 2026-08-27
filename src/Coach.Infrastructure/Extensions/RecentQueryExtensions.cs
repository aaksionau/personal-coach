using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Extensions;

internal static class RecentQueryExtensions
{
    /// <summary>
    /// Materialises the most recent <paramref name="count"/> rows of <paramref name="query"/> by
    /// <paramref name="orderBy"/> descending, then returns them oldest-first -- the "recent window"
    /// shape shared by the chat-message and reflection readers.
    /// </summary>
    public static async Task<IReadOnlyList<T>> ToRecentWindowAsync<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> orderBy,
        int count,
        CancellationToken cancellationToken)
    {
        var recentDescending = await query
            .OrderByDescending(orderBy)
            .Take(count)
            .ToListAsync(cancellationToken);

        recentDescending.Reverse();
        return recentDescending;
    }
}
