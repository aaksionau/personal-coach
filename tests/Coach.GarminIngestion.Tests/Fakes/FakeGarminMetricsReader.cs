using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.GarminIngestion.Tests.Fakes;

/// <summary>
/// Hand-rolled <see cref="IGarminMetricsReader"/> for the job tests: records the dates it was asked
/// for and either returns an empty snapshot per day or throws a configured exception (to exercise
/// the "a Garmin failure fails the Job" acceptance criterion).
/// </summary>
internal sealed class FakeGarminMetricsReader : IGarminMetricsReader
{
    public List<DateOnly> RequestedDates { get; } = [];

    public Exception? ThrowOnCollect { get; set; }

    public Task<GarminMetricsSnapshot> CollectDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        RequestedDates.Add(date);

        if (ThrowOnCollect is not null)
        {
            throw ThrowOnCollect;
        }

        var snapshot = new GarminMetricsSnapshot(
            new GarminDailyMetric { Date = date, IngestedAtUtc = DateTimeOffset.UtcNow },
            []);
        return Task.FromResult(snapshot);
    }
}
