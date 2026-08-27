using Coach.Application.Models;
using global::Garmin.Connect.Models;

namespace Coach.Infrastructure.Garmin.Tests;

public class GarminDailyMetricMapperTests
{
    private static readonly DateOnly Date = new(2026, 8, 26);
    private static readonly DateTimeOffset Ingested = new(2026, 8, 27, 6, 0, 0, TimeSpan.Zero);

    private static GarminMetricsSnapshot Map(GarminDaySnapshot snapshot) =>
        GarminDailyMetricMapper.Map(Date, snapshot, Ingested);

    [Fact]
    public void Map_ProjectsEveryDailySummaryField()
    {
        var snapshot = new GarminDaySnapshot(
            new GarminStats
            {
                TotalSteps = 8421,
                DailyStepGoal = 10000,
                RestingHeartRate = 52,
                AverageStressLevel = 34,
                MaxStressLevel = 91,
                BodyBatteryHighestValue = 88,
                BodyBatteryLowestValue = 21,
                BodyBatteryChargedValue = 61,
                BodyBatteryDrainedValue = 74,
                ModerateIntensityMinutes = 25,
                VigorousIntensityMinutes = 10,
            },
            SleepWith(dto => dto with
            {
                SleepTimeSeconds = 26_400, // 440 min
                DeepSleepSeconds = 5_400,  // 90 min
                RemSleepSeconds = 6_000,   // 100 min
                LightSleepSeconds = 14_400, // 240 min
                AwakeSleepSeconds = 600,   // 10 min
                SleepScores = new SleepScores { Overall = new Overall { Value = 76 } },
            }),
            []);

        var metric = Map(snapshot).Metric;

        Assert.Equal(Date, metric.Date);
        Assert.Equal(Ingested, metric.IngestedAtUtc);
        Assert.Equal(8421, metric.Steps);
        Assert.Equal(10000, metric.StepGoal);
        Assert.Equal(52, metric.RestingHeartRateBpm);
        Assert.Equal(440, metric.TotalSleepMinutes);
        Assert.Equal(90, metric.DeepSleepMinutes);
        Assert.Equal(100, metric.RemSleepMinutes);
        Assert.Equal(240, metric.LightSleepMinutes);
        Assert.Equal(10, metric.AwakeMinutes);
        Assert.Equal(76, metric.SleepScore);
        Assert.Equal(34, metric.AverageStressLevel);
        Assert.Equal(91, metric.MaxStressLevel);
        Assert.Equal(88, metric.BodyBatteryHigh);
        Assert.Equal(21, metric.BodyBatteryLow);
        Assert.Equal(61, metric.BodyBatteryCharged);
        Assert.Equal(74, metric.BodyBatteryDrained);
        Assert.Equal(25, metric.ModerateIntensityMinutes);
        Assert.Equal(10, metric.VigorousIntensityMinutes);
    }

    [Fact]
    public void Map_MapsActivities_SoonestFirst_WithConvertedUnits()
    {
        var snapshot = new GarminDaySnapshot(
            null,
            null,
            [
                new GarminActivity
                {
                    ActivityId = 222,
                    ActivityName = "Evening Strength",
                    ActivityType = new ActivityType { TypeKey = "strength_training" },
                    StartTimeGmt = new DateTime(2026, 8, 26, 18, 0, 0),
                    Duration = 1_800, // 30 min
                    Distance = 0,
                    Calories = 210,
                },
                new GarminActivity
                {
                    ActivityId = 111,
                    ActivityName = "  Morning Run  ",
                    ActivityType = new ActivityType { TypeKey = "running" },
                    StartTimeGmt = new DateTime(2026, 8, 26, 6, 30, 0),
                    Duration = 2_760, // 46 min
                    Distance = 8_200,
                    Calories = 540,
                },
            ]);

        var activities = Map(snapshot).Activities;

        Assert.Collection(
            activities,
            first =>
            {
                Assert.Equal(111, first.ActivityId);
                Assert.Equal("Morning Run", first.Name);
                Assert.Equal("running", first.ActivityType);
                Assert.Equal(Date, first.Date);
                Assert.Equal(new DateTimeOffset(2026, 8, 26, 6, 30, 0, TimeSpan.Zero), first.StartedAtUtc);
                Assert.Equal(46, first.DurationMinutes);
                Assert.Equal(8_200, first.DistanceMeters);
                Assert.Equal(540, first.Calories);
            },
            second =>
            {
                Assert.Equal(222, second.ActivityId);
                Assert.Equal("Evening Strength", second.Name);
                Assert.Null(second.DistanceMeters); // zero distance -> null
            });
    }

    [Fact]
    public void Map_LeavesFieldsNull_WhenGarminHasNoDataForTheDay()
    {
        var metric = Map(new GarminDaySnapshot(null, null, [])).Metric;

        Assert.Null(metric.Steps);
        Assert.Null(metric.RestingHeartRateBpm);
        Assert.Null(metric.TotalSleepMinutes);
        Assert.Null(metric.SleepScore);
        Assert.Null(metric.AverageStressLevel);
        Assert.Null(metric.BodyBatteryHigh);
        Assert.Empty(Map(new GarminDaySnapshot(null, null, [])).Activities);
    }

    [Fact]
    public void Map_TreatsZeroGaugeReadingsAsNoReading()
    {
        var snapshot = new GarminDaySnapshot(
            new GarminStats
            {
                TotalSteps = 0,
                RestingHeartRate = 0,
                AverageStressLevel = -1,
                BodyBatteryHighestValue = 0,
                BodyBatteryChargedValue = 0,
            },
            null,
            []);

        var metric = Map(snapshot).Metric;

        Assert.Equal(0, metric.Steps);                 // a counter -- a real zero
        Assert.Equal(0, metric.BodyBatteryCharged);    // a counter -- a real zero
        Assert.Null(metric.RestingHeartRateBpm);       // a gauge -- no reading
        Assert.Null(metric.AverageStressLevel);        // negative sentinel -- no reading
        Assert.Null(metric.BodyBatteryHigh);           // a gauge -- no reading
    }

    [Fact]
    public void Map_HandlesSleepDataWithNoDailyDto()
    {
        var snapshot = new GarminDaySnapshot(null, new GarminSleepData(), []);

        var metric = Map(snapshot).Metric;

        Assert.Null(metric.TotalSleepMinutes);
        Assert.Null(metric.SleepScore);
    }

    [Fact]
    public void Map_FallsBackToPlaceholders_ForMissingActivityNameAndType()
    {
        var snapshot = new GarminDaySnapshot(
            null,
            null,
            [
                new GarminActivity
                {
                    ActivityId = 5,
                    ActivityName = null!,
                    ActivityType = null!,
                    StartTimeGmt = new DateTime(2026, 8, 26, 12, 0, 0),
                    Duration = 600,
                },
            ]);

        var activity = Assert.Single(Map(snapshot).Activities);

        Assert.Equal("(unnamed activity)", activity.Name);
        Assert.Equal("unknown", activity.ActivityType);
    }

    private static GarminSleepData SleepWith(Func<DailySleepDto, DailySleepDto> configure) =>
        new() { DailySleepDto = configure(new DailySleepDto()) };
}
