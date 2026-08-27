using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeCalendarReader : ICalendarReader
{
    public IReadOnlyList<CalendarEvent> EventsToReturn { get; set; } = [];

    public int? LastRequestedWithinDays { get; private set; }

    public Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int withinDays, CancellationToken cancellationToken)
    {
        LastRequestedWithinDays = withinDays;
        return Task.FromResult(EventsToReturn);
    }
}
