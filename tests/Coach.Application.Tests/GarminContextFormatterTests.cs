using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class GarminContextFormatterTests
{
    [Fact]
    public void Format_ReportsNothingIngestedYet_WhenSnapshotIsNull()
    {
        Assert.Equal("Latest Garmin metrics: none available yet.", GarminContextFormatter.Format(null));
    }

    [Fact]
    public void Format_RendersEveryMetricSlice()
    {
        var snapshot = new GarminMetricsSnapshot(
            new GarminDailyMetric
            {
                Date = new DateOnly(2026, 8, 26),
                Steps = 8421,
                StepGoal = 10000,
                RestingHeartRateBpm = 52,
                TotalSleepMinutes = 440,
                DeepSleepMinutes = 90,
                RemSleepMinutes = 100,
                LightSleepMinutes = 240,
                AwakeMinutes = 10,
                SleepScore = 76,
                AverageStressLevel = 34,
                MaxStressLevel = 91,
                BodyBatteryHigh = 88,
                BodyBatteryLow = 21,
                BodyBatteryCharged = 61,
                BodyBatteryDrained = 74,
                ModerateIntensityMinutes = 25,
                VigorousIntensityMinutes = 10,
                IngestedAtUtc = DateTimeOffset.UtcNow,
            },
            [
                new GarminActivitySummary
                {
                    ActivityId = 1,
                    Date = new DateOnly(2026, 8, 26),
                    Name = "Morning Run",
                    ActivityType = "running",
                    StartedAtUtc = new DateTimeOffset(2026, 8, 26, 6, 30, 0, TimeSpan.Zero),
                    DurationMinutes = 46,
                    DistanceMeters = 8200,
                    Calories = 540,
                },
            ]);

        var text = GarminContextFormatter.Format(snapshot);

        Assert.Contains("Latest Garmin metrics for 2026-08-26", text);
        Assert.Contains("Steps: 8421 (goal 10000)", text);
        Assert.Contains("Sleep: 7h 20m", text);
        Assert.Contains("score 76", text);
        Assert.Contains("Resting heart rate: 52 bpm", text);
        Assert.Contains("Stress: avg 34, max 91", text);
        Assert.Contains("Body battery: 21-88", text);
        Assert.Contains("Intensity minutes: 25 moderate, 10 vigorous", text);
        Assert.Contains("Morning Run (running)", text);
        Assert.Contains("8.2 km", text);
    }

    [Fact]
    public void Format_OmitsLinesWithNoReading_AndNotesNoWorkouts()
    {
        var snapshot = new GarminMetricsSnapshot(
            new GarminDailyMetric
            {
                Date = new DateOnly(2026, 8, 26),
                Steps = 3000,
                IngestedAtUtc = DateTimeOffset.UtcNow,
            },
            []);

        var text = GarminContextFormatter.Format(snapshot);

        Assert.Contains("Steps: 3000", text);
        Assert.DoesNotContain("Sleep:", text);
        Assert.DoesNotContain("Resting heart rate", text);
        Assert.DoesNotContain("Body battery", text);
        Assert.Contains("Workouts: none logged.", text);
    }

    [Fact]
    public void Format_ReportsIngestionTime_AndFlagsAnInProgressDay()
    {
        var completeDay = GarminContextFormatter.Format(new GarminMetricsSnapshot(
            new GarminDailyMetric
            {
                Date = new DateOnly(2026, 8, 26),
                Steps = 9000,
                IngestedAtUtc = new DateTimeOffset(2026, 8, 27, 6, 15, 0, TimeSpan.Zero),
            },
            []));
        Assert.Contains("ingested 2026-08-27 06:15 UTC", completeDay);
        Assert.DoesNotContain("still being recorded", completeDay);

        var inProgressDay = GarminContextFormatter.Format(new GarminMetricsSnapshot(
            new GarminDailyMetric
            {
                Date = new DateOnly(2026, 8, 27),
                Steps = 900,
                IngestedAtUtc = new DateTimeOffset(2026, 8, 27, 6, 15, 0, TimeSpan.Zero),
            },
            []));
        Assert.Contains("today so far, still being recorded", inProgressDay);
    }
}
