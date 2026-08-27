using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>
/// A day's Garmin summary plus that day's activities -- the composed view the Health coach's
/// context reads, assembled from the two flat entities in the Application layer rather than via
/// EF navigation (like <see cref="GoalWithActionItems"/>).
/// </summary>
public sealed record GarminMetricsSnapshot(
    GarminDailyMetric Metric,
    IReadOnlyList<GarminActivitySummary> Activities);
