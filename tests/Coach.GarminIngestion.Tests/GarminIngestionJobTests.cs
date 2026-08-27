using Coach.GarminIngestion.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace Coach.GarminIngestion.Tests;

public class GarminIngestionJobTests
{
    private static GarminIngestionJob JobFor(FakeGarminMetricsReader reader, FakeGarminMetricsStore store) =>
        new(reader, store, NullLogger<GarminIngestionJob>.Instance);

    [Fact]
    public async Task RunAsync_IngestsARollingWindow_AndUpsertsEachDay()
    {
        var reader = new FakeGarminMetricsReader();
        var store = new FakeGarminMetricsStore();

        var exitCode = await JobFor(reader, store).RunAsync(CancellationToken.None);

        Assert.Equal(0, exitCode);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var expected = new[] { today.AddDays(-2), today.AddDays(-1), today };
        Assert.Equal(expected, reader.RequestedDates);
        Assert.Equal(expected, store.UpsertedDates);
    }

    [Fact]
    public async Task RunAsync_ReturnsNonZero_WhenGarminFails_WithoutTouchingTheStoreFurther()
    {
        var reader = new FakeGarminMetricsReader { ThrowOnCollect = new HttpRequestException("garmin login failed") };
        var store = new FakeGarminMetricsStore();

        var exitCode = await JobFor(reader, store).RunAsync(CancellationToken.None);

        Assert.Equal(1, exitCode);
        Assert.Empty(store.UpsertedDates);
    }
}
