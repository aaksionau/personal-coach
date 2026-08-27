using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeGarminMetricsStore : IGarminMetricsStore
{
    public GarminMetricsSnapshot? SnapshotToReturn { get; set; }

    public bool GetLatestWasCalled { get; private set; }

    public GarminDailyMetric? LastUpsertedMetric { get; private set; }

    public IReadOnlyList<GarminActivitySummary>? LastUpsertedActivities { get; private set; }

    public Task UpsertAsync(
        GarminDailyMetric metric,
        IReadOnlyList<GarminActivitySummary> activities,
        CancellationToken cancellationToken)
    {
        LastUpsertedMetric = metric;
        LastUpsertedActivities = activities;
        return Task.CompletedTask;
    }

    public Task<GarminMetricsSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        GetLatestWasCalled = true;
        return Task.FromResult(SnapshotToReturn);
    }
}
