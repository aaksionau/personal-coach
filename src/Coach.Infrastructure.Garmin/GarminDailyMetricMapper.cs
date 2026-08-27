using Coach.Application.Models;
using Coach.Domain.Entities;
using global::Garmin.Connect.Models;

namespace Coach.Infrastructure.Garmin;

/// <summary>
/// Normalizes a day's raw Garmin Connect responses (<see cref="GarminDaySnapshot"/>) into the
/// app's <see cref="GarminMetricsSnapshot"/>. Pure and network-free -- the ingestion timestamp is
/// passed in rather than read from the clock -- so it can be exercised directly in tests, the
/// mapping the acceptance criteria call out.
///
/// Rules: seconds are rounded to whole minutes; gauge readings (resting HR, stress, body-battery
/// high/low) that come back as zero or negative are treated as "no reading" (<c>null</c>), while
/// counters (steps, charged/drained, intensity minutes) keep a real zero; an absent
/// <see cref="GarminDaySnapshot.Summary"/> or sleep DTO simply leaves those fields null.
/// </summary>
internal static class GarminDailyMetricMapper
{
    public static GarminMetricsSnapshot Map(DateOnly date, GarminDaySnapshot snapshot, DateTimeOffset ingestedAtUtc)
    {
        var summary = snapshot.Summary;
        var sleep = snapshot.Sleep?.DailySleepDto;

        var metric = new GarminDailyMetric
        {
            Date = date,
            Steps = summary is null ? null : (int)summary.TotalSteps,
            StepGoal = summary is null ? null : PositiveOrNull(summary.DailyStepGoal),
            RestingHeartRateBpm = summary is null ? null : PositiveOrNull(summary.RestingHeartRate),
            TotalSleepMinutes = sleep is null ? null : ToMinutes(sleep.SleepTimeSeconds),
            DeepSleepMinutes = sleep is null ? null : ToMinutes(sleep.DeepSleepSeconds),
            RemSleepMinutes = sleep is null ? null : ToMinutes(sleep.RemSleepSeconds),
            LightSleepMinutes = sleep is null ? null : ToMinutes(sleep.LightSleepSeconds),
            AwakeMinutes = sleep is null ? null : ToMinutes(sleep.AwakeSleepSeconds),
            SleepScore = PositiveOrNull(sleep?.SleepScores?.Overall?.Value),
            AverageStressLevel = summary is null ? null : PositiveOrNull(summary.AverageStressLevel),
            MaxStressLevel = summary is null ? null : PositiveOrNull(summary.MaxStressLevel),
            BodyBatteryHigh = summary is null ? null : PositiveOrNull(summary.BodyBatteryHighestValue),
            BodyBatteryLow = summary is null ? null : PositiveOrNull(summary.BodyBatteryLowestValue),
            BodyBatteryCharged = summary is null ? null : (int)summary.BodyBatteryChargedValue,
            BodyBatteryDrained = summary is null ? null : (int)summary.BodyBatteryDrainedValue,
            ModerateIntensityMinutes = summary is null ? null : (int)summary.ModerateIntensityMinutes,
            VigorousIntensityMinutes = summary is null ? null : (int)summary.VigorousIntensityMinutes,
            IngestedAtUtc = ingestedAtUtc,
        };

        var activities = snapshot.Activities
            .Select(activity => MapActivity(date, activity))
            .OrderBy(activity => activity.StartedAtUtc)
            .ToList();

        return new GarminMetricsSnapshot(metric, activities);
    }

    private static GarminActivitySummary MapActivity(DateOnly date, GarminActivity activity) => new()
    {
        ActivityId = activity.ActivityId,
        Date = date,
        Name = string.IsNullOrWhiteSpace(activity.ActivityName) ? "(unnamed activity)" : activity.ActivityName.Trim(),
        ActivityType = string.IsNullOrWhiteSpace(activity.ActivityType?.TypeKey) ? "unknown" : activity.ActivityType.TypeKey,
        StartedAtUtc = new DateTimeOffset(DateTime.SpecifyKind(activity.StartTimeGmt, DateTimeKind.Utc)),
        DurationMinutes = PositiveOrNull((long)Math.Round(activity.Duration / 60.0)),
        DistanceMeters = PositiveOrNull((long)Math.Round(activity.Distance)),
        Calories = PositiveOrNull((long)Math.Round(activity.Calories)),
    };

    private static int ToMinutes(long seconds) => (int)Math.Round(seconds / 60.0);

    private static int? PositiveOrNull(long value) => value > 0 ? (int)value : null;

    private static int? PositiveOrNull(long? value) => value is > 0 ? (int)value.Value : null;
}
