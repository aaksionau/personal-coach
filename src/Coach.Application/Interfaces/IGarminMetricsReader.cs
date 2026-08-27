using Coach.Application.Models;

namespace Coach.Application.Interfaces;

/// <summary>
/// The seam between the Garmin ingestion CronJob and the Garmin Connect adapter: pulls one day's
/// summary from Garmin and maps it into the app's daily-metric shape. Unlike
/// <see cref="ICalendarReader"/>, this <b>may throw</b> -- the CronJob is an isolated failure
/// domain, so a bad login or an API-shape change should surface as a failed Kubernetes Job, not
/// be silently swallowed.
/// </summary>
public interface IGarminMetricsReader
{
    Task<GarminMetricsSnapshot> CollectDayAsync(DateOnly date, CancellationToken cancellationToken);
}
