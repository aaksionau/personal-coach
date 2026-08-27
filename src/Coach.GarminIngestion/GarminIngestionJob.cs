using Coach.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Coach.GarminIngestion;

/// <summary>
/// The daily ingestion run: pull the last few days from Garmin Connect and upsert them into the
/// coach Postgres, then exit. Thin orchestration over the collector and the store -- schema
/// migration is handled by the composition root (<c>Program</c>), not here, so this is testable
/// with a faked reader and store.
/// </summary>
internal sealed class GarminIngestionJob(
    IGarminMetricsReader reader,
    IGarminMetricsStore store,
    ILogger<GarminIngestionJob> logger)
{
    /// <summary>
    /// Offsets from "today" (UTC) to ingest each run. Garmin keys its daily summaries by the
    /// account's local calendar date, which can be a day either side of the UTC date, so we always
    /// pull a few days back: -1 is the last complete day for the model to ground on, 0 gives a
    /// same-day chat something, and -2 both covers the timezone skew and lets a missed run catch up.
    /// Re-running the job simply refreshes each day.
    /// </summary>
    private static readonly int[] DayOffsets = [-2, -1, 0];

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        try
        {
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
