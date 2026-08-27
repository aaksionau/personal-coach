using Coach.Infrastructure.GoogleCalendar;
using Google.Apis.Calendar.v3.Data;

namespace Coach.Infrastructure.GoogleCalendar.Tests.Fakes;

internal sealed class FakeGoogleCalendarEvents : IGoogleCalendarEvents
{
    public Events Response { get; set; } = new() { Items = [] };

    public Exception? ThrowOnList { get; set; }

    public bool WasCalled { get; private set; }

    public DateTimeOffset LastTimeMin { get; private set; }

    public DateTimeOffset LastTimeMax { get; private set; }

    public Task<Events> ListAsync(DateTimeOffset timeMin, DateTimeOffset timeMax, CancellationToken cancellationToken)
    {
        WasCalled = true;
        LastTimeMin = timeMin;
        LastTimeMax = timeMax;

        if (ThrowOnList is not null)
        {
            throw ThrowOnList;
        }

        return Task.FromResult(Response);
    }
}
