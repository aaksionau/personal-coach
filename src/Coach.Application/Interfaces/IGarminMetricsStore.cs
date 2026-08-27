using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

/// <summary>
/// Persistence port for the Garmin daily metrics table. The standalone ingestion CronJob writes
/// through <see cref="UpsertAsync"/>; the Health coach's <c>CoachContextBuilder</c> reads the
/// latest day through <see cref="GetLatestAsync"/>. The main app never calls Garmin directly.
/// </summary>
public interface IGarminMetricsStore
{
    /// <summary>
    /// Replaces the stored metrics (and activities) for <paramref name="metric"/>'s date, so a day
    /// can be re-ingested idempotently.
    /// </summary>
    Task UpsertAsync(
        GarminDailyMetric metric,
        IReadOnlyList<GarminActivitySummary> activities,
        CancellationToken cancellationToken);

    /// <summary>The most recent day's metrics, or <c>null</c> when nothing has been ingested yet.</summary>
    Task<GarminMetricsSnapshot?> GetLatestAsync(CancellationToken cancellationToken);
}
