using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Stores;

/// <summary>
/// EF Core adapter for <see cref="IGarminMetricsStore"/>. The write side is used only by the
/// standalone ingestion CronJob; the read side (<see cref="GetLatestAsync"/>) by the Health
/// coach's <c>CoachContextBuilder</c>.
/// </summary>
public sealed class GarminMetricsStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IGarminMetricsStore
{
    public async Task UpsertAsync(
        GarminDailyMetric metric,
        IReadOnlyList<GarminActivitySummary> activities,
        CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        // Replace the day wholesale -- simplest way to keep a re-ingested day idempotent without
        // diffing individual activities. ExecuteDeleteAsync doesn't cascade, so clear children first.
        await dbContext.GarminActivitySummaries
            .Where(a => a.Date == metric.Date)
            .ExecuteDeleteAsync(cancellationToken);
        await dbContext.GarminDailyMetrics
            .Where(m => m.Date == metric.Date)
            .ExecuteDeleteAsync(cancellationToken);

        dbContext.GarminDailyMetrics.Add(metric);
        dbContext.GarminActivitySummaries.AddRange(activities);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<GarminMetricsSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var metric = await dbContext.GarminDailyMetrics
            .AsNoTracking()
            .OrderByDescending(m => m.Date)
            .FirstOrDefaultAsync(cancellationToken);
        if (metric is null)
        {
            return null;
        }

        var activities = await dbContext.GarminActivitySummaries
            .AsNoTracking()
            .Where(a => a.Date == metric.Date)
            .OrderBy(a => a.StartedAtUtc)
            .ToListAsync(cancellationToken);

        return new GarminMetricsSnapshot(metric, activities);
    }
}
