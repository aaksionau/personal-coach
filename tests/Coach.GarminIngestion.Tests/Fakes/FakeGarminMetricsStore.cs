using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.GarminIngestion.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IGarminMetricsStore"/> for the job tests: records every day it was asked
/// to upsert. The read side is unused here (the job only writes).
/// </summary>
internal sealed class FakeGarminMetricsStore : IGarminMetricsStore
{
    public List<DateOnly> UpsertedDates { get; } = [];

    public Task UpsertAsync(
        GarminDailyMetric metric,
        IReadOnlyList<GarminActivitySummary> activities,
        CancellationToken cancellationToken)
    {
        UpsertedDates.Add(metric.Date);
        return Task.CompletedTask;
    }

    public Task<GarminMetricsSnapshot?> GetLatestAsync(CancellationToken cancellationToken) =>
        Task.FromResult<GarminMetricsSnapshot?>(null);
}
