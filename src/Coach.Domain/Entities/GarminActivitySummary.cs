namespace Coach.Domain.Entities;

/// <summary>
/// One Garmin activity/workout on a given day -- a child of <see cref="GarminDailyMetric"/> via
/// the flat <see cref="Date"/> foreign key (no navigation property, matching the rest of the
/// domain). Keyed by Garmin's own <see cref="ActivityId"/> so re-ingesting a day doesn't
/// duplicate rows.
/// </summary>
public sealed class GarminActivitySummary
{
    /// <summary>Garmin Connect's own activity id. Primary key.</summary>
    public required long ActivityId { get; init; }

    /// <summary>The metric day this activity belongs to -- foreign key to <see cref="GarminDailyMetric.Date"/>.</summary>
    public required DateOnly Date { get; init; }

    public required string Name { get; init; }

    /// <summary>Garmin's activity type key, e.g. "running", "strength_training".</summary>
    public required string ActivityType { get; init; }

    public required DateTimeOffset StartedAtUtc { get; init; }

    public int? DurationMinutes { get; init; }

    public int? DistanceMeters { get; init; }

    public int? Calories { get; init; }
}
