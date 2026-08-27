using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ReflectionStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IReflectionStore
{
    public async Task AddAsync(Reflection reflection, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Reflections.Add(reflection);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reflection>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var recentDescending = await dbContext.Reflections
            .AsNoTracking()
            .Where(r => r.CoachSlug == coachSlug)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

        recentDescending.Reverse();
        return recentDescending;
    }
}
