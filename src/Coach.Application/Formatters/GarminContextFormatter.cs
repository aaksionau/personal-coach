using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Formatters;

/// <summary>
/// Formats the latest Garmin daily metrics as text for the model's system prompt -- steps, sleep,
/// resting heart rate, stress, body battery, intensity minutes, and the day's activities. Only
/// stitched into the Health coach's prompt (see <see cref="CoachSystemPromptComposer"/>); the
/// other three personas never see a Garmin slice. Read-only: the model is told it cannot act on
/// these. Every line is omitted when Garmin has no reading for it, so a sparse day stays terse.
///
/// The header carries the ingestion time and flags an in-progress day (a row whose date matches
/// the day it was ingested on), so the model doesn't read a half-recorded "today" -- few steps,
/// no sleep yet -- as a complete picture. The ingestion job re-pulls the last few days each run,
/// so the previous day's row lands complete on the next run.
/// </summary>
public static class GarminContextFormatter
{
    public static string Format(GarminMetricsSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return "Latest Garmin metrics: none available yet.";
        }

        var metric = snapshot.Metric;
        var ingestedOn = DateOnly.FromDateTime(metric.IngestedAtUtc.UtcDateTime);
        var dayNote = metric.Date >= ingestedOn
            ? " -- today so far, still being recorded"
            : string.Empty;
        var builder = new StringBuilder(
            $"Latest Garmin metrics for {metric.Date:yyyy-MM-dd}{dayNote} "
            + $"(ingested {metric.IngestedAtUtc.UtcDateTime:yyyy-MM-dd HH:mm} UTC; read-only -- grounding for health advice, you cannot change these):\n");

        if (metric.Steps is { } steps)
        {
            builder.Append("- Steps: ").Append(steps);
            if (metric.StepGoal is { } goal)
            {
                builder.Append(" (goal ").Append(goal).Append(')');
            }

            builder.Append('\n');
        }

        AppendSleep(builder, metric);

        if (metric.RestingHeartRateBpm is { } rhr)
        {
            builder.Append("- Resting heart rate: ").Append(rhr).Append(" bpm\n");
        }

        AppendStress(builder, metric);
        AppendBodyBattery(builder, metric);
        AppendIntensityMinutes(builder, metric);
        AppendActivities(builder, snapshot.Activities);

        return builder.ToString();
    }

    private static void AppendSleep(StringBuilder builder, GarminDailyMetric metric)
    {
        if (metric.TotalSleepMinutes is not { } total)
        {
            return;
        }

        builder.Append("- Sleep: ").Append(FormatHoursMinutes(total));

        var stages = new List<string>(4);
        if (metric.DeepSleepMinutes is { } deep) stages.Add($"deep {FormatHoursMinutes(deep)}");
        if (metric.RemSleepMinutes is { } rem) stages.Add($"REM {FormatHoursMinutes(rem)}");
        if (metric.LightSleepMinutes is { } light) stages.Add($"light {FormatHoursMinutes(light)}");
        if (metric.AwakeMinutes is { } awake) stages.Add($"awake {FormatHoursMinutes(awake)}");
        if (stages.Count > 0)
        {
            builder.Append(" (").Append(string.Join(", ", stages)).Append(')');
        }

        if (metric.SleepScore is { } score)
        {
            builder.Append(", score ").Append(score);
        }

        builder.Append('\n');
    }

    private static void AppendStress(StringBuilder builder, GarminDailyMetric metric)
    {
        if (metric.AverageStressLevel is not { } avg)
        {
            return;
        }

        builder.Append("- Stress: avg ").Append(avg);
        if (metric.MaxStressLevel is { } max)
        {
            builder.Append(", max ").Append(max);
        }

        builder.Append(" (0-100)\n");
    }

    private static void AppendBodyBattery(StringBuilder builder, GarminDailyMetric metric)
    {
        if (metric is { BodyBatteryHigh: null, BodyBatteryLow: null })
        {
            return;
        }

        builder.Append("- Body battery: ")
            .Append(metric.BodyBatteryLow is { } lo ? lo.ToString() : "?").Append('-')
            .Append(metric.BodyBatteryHigh is { } hi ? hi.ToString() : "?");

        var deltas = new List<string>(2);
        if (metric.BodyBatteryCharged is { } charged) deltas.Add($"+{charged} charged");
        if (metric.BodyBatteryDrained is { } drained) deltas.Add($"-{drained} drained");
        if (deltas.Count > 0)
        {
            builder.Append(" (").Append(string.Join(", ", deltas)).Append(')');
        }

        builder.Append('\n');
    }

    private static void AppendIntensityMinutes(StringBuilder builder, GarminDailyMetric metric)
    {
        if (metric is { ModerateIntensityMinutes: null, VigorousIntensityMinutes: null })
        {
            return;
        }

        builder.Append("- Intensity minutes: ")
            .Append(metric.ModerateIntensityMinutes ?? 0).Append(" moderate, ")
            .Append(metric.VigorousIntensityMinutes ?? 0).Append(" vigorous\n");
    }

    private static void AppendActivities(StringBuilder builder, IReadOnlyList<GarminActivitySummary> activities)
    {
        if (activities.Count == 0)
        {
            builder.Append("- Workouts: none logged.\n");
            return;
        }

        builder.Append("- Workouts:\n");
        foreach (var activity in activities)
        {
            builder.Append("  - ").Append(activity.Name).Append(" (").Append(activity.ActivityType).Append(')');

            var parts = new List<string>(2);
            if (activity.DurationMinutes is { } minutes) parts.Add(FormatHoursMinutes(minutes));
            if (activity.DistanceMeters is { } meters and > 0) parts.Add($"{meters / 1000.0:0.0} km");
            if (parts.Count > 0)
            {
                builder.Append(" -- ").Append(string.Join(", ", parts));
            }

            builder.Append('\n');
        }
    }

    private static string FormatHoursMinutes(int totalMinutes)
    {
        var hours = totalMinutes / 60;
        var minutes = totalMinutes % 60;
        return hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
    }
}
