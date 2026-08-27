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

        // summary?/sleep? -- an absent DTO leaves every field it feeds null; the nullable helper
        // overloads pass a null straight through.
        var metric = new GarminDailyMetric
        {
            Date = date,
            Steps = (int?)summary?.TotalSteps,
            StepGoal = PositiveOrNull(summary?.DailyStepGoal),
            RestingHeartRateBpm = PositiveOrNull(summary?.RestingHeartRate),
            TotalSleepMinutes = ToMinutes(sleep?.SleepTimeSeconds),
            DeepSleepMinutes = ToMinutes(sleep?.DeepSleepSeconds),
            RemSleepMinutes = ToMinutes(sleep?.RemSleepSeconds),
            LightSleepMinutes = ToMinutes(sleep?.LightSleepSeconds),
            AwakeMinutes = ToMinutes(sleep?.AwakeSleepSeconds),
            SleepScore = PositiveOrNull(sleep?.SleepScores?.Overall?.Value),
            AverageStressLevel = PositiveOrNull(summary?.AverageStressLevel),
            MaxStressLevel = PositiveOrNull(summary?.MaxStressLevel),
            BodyBatteryHigh = PositiveOrNull(summary?.BodyBatteryHighestValue),
            BodyBatteryLow = PositiveOrNull(summary?.BodyBatteryLowestValue),
            BodyBatteryCharged = (int?)summary?.BodyBatteryChargedValue,
            BodyBatteryDrained = (int?)summary?.BodyBatteryDrainedValue,
            ModerateIntensityMinutes = (int?)summary?.ModerateIntensityMinutes,
            VigorousIntensityMinutes = (int?)summary?.VigorousIntensityMinutes,
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

    private static int? ToMinutes(long? seconds) => seconds is { } value ? ToMinutes(value) : null;

    private static int? PositiveOrNull(long value) => value > 0 ? (int)value : null;

    private static int? PositiveOrNull(long? value) => value is > 0 ? (int)value.Value : null;
}
