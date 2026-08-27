namespace Coach.Infrastructure.Garmin;

/// <summary>
/// Narrow seam over the Garmin Connect SDK (whose own interface is ~50 methods): fetches the
/// summary, sleep, and activities for one day. Split out so <see cref="GarminMetricsCollector"/>'s
/// date handling and <see cref="GarminDailyMetricMapper"/>'s normalization can be tested against a
/// fake with no network. Read-only by construction -- no write method here.
/// </summary>
internal interface IGarminApi
{
    Task<GarminDaySnapshot> GetDayAsync(DateOnly date, CancellationToken cancellationToken);
}
