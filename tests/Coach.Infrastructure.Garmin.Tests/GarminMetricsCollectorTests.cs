using Coach.Infrastructure.Garmin.Tests.Fakes;
using global::Garmin.Connect.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Garmin.Tests;

public class GarminMetricsCollectorTests
{
    private static readonly GarminOptions Configured = new() { Email = "user@example.com", Password = "pw" };
    private static readonly DateOnly Date = new(2026, 8, 26);

    [Fact]
    public async Task CollectDayAsync_RequestsTheGivenDate_AndReturnsTheMappedSnapshot()
    {
        var api = new FakeGarminApi
        {
            Response = new GarminDaySnapshot(new GarminStats { TotalSteps = 1234 }, null, []),
        };

        var snapshot = await CollectorFor(api, Configured).CollectDayAsync(Date, CancellationToken.None);

        Assert.Equal(Date, api.LastRequestedDate);
        Assert.Equal(1234, snapshot.Metric.Steps);
        Assert.Equal(Date, snapshot.Metric.Date);
    }

    [Fact]
    public async Task CollectDayAsync_Throws_AndSkipsTheApi_WhenNotConfigured()
    {
        var api = new FakeGarminApi();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CollectorFor(api, new GarminOptions()).CollectDayAsync(Date, CancellationToken.None));
        Assert.False(api.WasCalled);
    }

    [Fact]
    public async Task CollectDayAsync_PropagatesApiFailures()
    {
        var api = new FakeGarminApi { ThrowOnGet = new HttpRequestException("garmin login failed") };

        await Assert.ThrowsAsync<HttpRequestException>(
            () => CollectorFor(api, Configured).CollectDayAsync(Date, CancellationToken.None));
    }

    [Fact]
    public async Task CollectDayAsync_PropagatesCancellation()
    {
        var api = new FakeGarminApi { ThrowOnGet = new OperationCanceledException() };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => CollectorFor(api, Configured).CollectDayAsync(Date, CancellationToken.None));
    }

    private static GarminMetricsCollector CollectorFor(IGarminApi api, GarminOptions options) =>
        new(api, Options.Create(options), NullLogger<GarminMetricsCollector>.Instance);
}
