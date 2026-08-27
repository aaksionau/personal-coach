using Coach.Application.Interfaces;
using Coach.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Garmin;

/// <summary>
/// <see cref="IGarminMetricsReader"/> backed by Garmin Connect. Fetches one day through
/// <see cref="IGarminApi"/> and normalizes it via <see cref="GarminDailyMetricMapper"/>.
/// Unlike the calendar reader this <b>does not swallow failures</b> -- the ingestion CronJob is
/// an isolated failure domain, so a bad login or an API-shape change should fail the k8s Job
/// loudly rather than silently write nothing.
/// </summary>
internal sealed class GarminMetricsCollector(
    IGarminApi api,
    IOptions<GarminOptions> options,
    ILogger<GarminMetricsCollector> logger) : IGarminMetricsReader
{
    public async Task<GarminMetricsSnapshot> CollectDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException(
                "Garmin is not configured -- set Garmin:Email and Garmin:Password (Garmin__Email / Garmin__Password).");
        }

        logger.LogInformation("Fetching Garmin metrics for {Date}.", date);
        var day = await api.GetDayAsync(date, cancellationToken);
        var snapshot = GarminDailyMetricMapper.Map(date, day, DateTimeOffset.UtcNow);
        logger.LogInformation(
            "Fetched Garmin metrics for {Date}: {Steps} steps, {Activities} activities.",
            date, snapshot.Metric.Steps, snapshot.Activities.Count);
        return snapshot;
    }
}
