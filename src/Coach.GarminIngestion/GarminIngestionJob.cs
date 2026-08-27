using Coach.Application.Interfaces;
using Coach.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Coach.GarminIngestion;

/// <summary>
/// The daily ingestion run: pull the last couple of days from Garmin Connect and upsert them into
/// the coach Postgres, then exit. Thin orchestration over already-tested pieces (the collector and
/// the store) -- deliberately not unit-tested, matching the PRD's stance on the Check-in Scheduler.
/// </summary>
internal sealed class GarminIngestionJob(
    IGarminMetricsReader reader,
    IGarminMetricsStore store,
    IDbContextFactory<CoachDbContext> dbContextFactory,
    ILogger<GarminIngestionJob> logger)
{
    /// <summary>
    /// Yesterday is the last fully-recorded day; today is pulled too so a same-day chat has
    /// something, and re-running the job simply refreshes both days.
    /// </summary>
    private static readonly int[] DayOffsets = [-1, 0];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Best-effort, like Coach.Web's startup migration: the job may deploy ahead of the web
            // app, and applying an already-applied migration is a no-op.
            await using (var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken))
            {
                await dbContext.Database.MigrateAsync(cancellationToken);
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var offset in DayOffsets)
            {
                var date = today.AddDays(offset);
                var snapshot = await reader.CollectDayAsync(date, cancellationToken);
                await store.UpsertAsync(snapshot.Metric, snapshot.Activities, cancellationToken);
                logger.LogInformation(
                    "Stored Garmin metrics for {Date} ({Activities} activities).", date, snapshot.Activities.Count);
            }

            return 0;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Garmin ingestion failed.");
            return 1;
        }
    }
}
