namespace Coach.Infrastructure.Garmin.Tests.Fakes;

internal sealed class FakeGarminApi : IGarminApi
{
    public GarminDaySnapshot Response { get; set; } = new(null, null, []);

    public Exception? ThrowOnGet { get; set; }

    public bool WasCalled { get; private set; }

    public DateOnly LastRequestedDate { get; private set; }

    public Task<GarminDaySnapshot> GetDayAsync(DateOnly date, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastRequestedDate = date;

        if (ThrowOnGet is not null)
        {
            throw ThrowOnGet;
        }

        return Task.FromResult(Response);
    }
}
