namespace Coach.Domain.Entities;

/// <summary>
/// One day's Garmin Connect summary for the user, written by the standalone Garmin ingestion
/// CronJob and read by the Health coach's context. One row per <see cref="Date"/>; a day is
/// re-ingested by replacing its row (and its <see cref="GarminActivitySummary"/> children).
/// Every metric is nullable -- Garmin may have no reading for a given field on a given day, and
/// a missing value must stay distinct from a real zero. Instances are built solely by the
/// ingestion mapper via an object initializer, so there is no <c>Create</c> factory: an
/// ~18-argument factory would add nothing over the initializer (compare
/// <see cref="ValuesProfile"/>, which is special-cased in the other direction).
/// </summary>
public sealed class GarminDailyMetric
{
    /// <summary>The calendar date these metrics describe. Primary key.</summary>
    public required DateOnly Date { get; init; }

    public int? Steps { get; init; }

    public int? StepGoal { get; init; }

    public int? RestingHeartRateBpm { get; init; }

    public int? TotalSleepMinutes { get; init; }

    public int? DeepSleepMinutes { get; init; }

    public int? RemSleepMinutes { get; init; }

    public int? LightSleepMinutes { get; init; }

    public int? AwakeMinutes { get; init; }

    public int? SleepScore { get; init; }

    public int? AverageStressLevel { get; init; }

    public int? MaxStressLevel { get; init; }

    public int? BodyBatteryHigh { get; init; }

    public int? BodyBatteryLow { get; init; }

    public int? BodyBatteryCharged { get; init; }

    public int? BodyBatteryDrained { get; init; }

    public int? ModerateIntensityMinutes { get; init; }

    public int? VigorousIntensityMinutes { get; init; }

    /// <summary>When the ingestion job wrote this row -- lets the coach judge how fresh the data is.</summary>
    public required DateTimeOffset IngestedAtUtc { get; init; }
}
