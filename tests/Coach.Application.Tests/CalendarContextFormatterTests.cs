using Coach.Application.Formatters;
using Coach.Application.Models;

namespace Coach.Application.Tests;

public class CalendarContextFormatterTests
{
    [Fact]
    public void Format_ReturnsAPlaceholderNamingTheWindow_WhenThereAreNoEvents()
    {
        var result = CalendarContextFormatter.Format([], withinDays: 7);

        Assert.Equal("Upcoming calendar events: none in the next 7 days.", result);
    }

    [Fact]
    public void Format_RendersATimedEvent_WithDateAndTime()
    {
        var timed = new CalendarEvent(
            "Standup", new DateTimeOffset(2026, 8, 28, 9, 15, 0, TimeSpan.Zero), null, IsAllDay: false, Location: null);

        var result = CalendarContextFormatter.Format([timed], withinDays: 7);

        Assert.Contains("- (2026-08-28 09:15) Standup", result);
    }

    [Fact]
    public void Format_MarksAnAllDayEvent_AndAppendsLocation()
    {
        var allDay = new CalendarEvent(
            "Conference", new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero), null, IsAllDay: true, Location: "Berlin");

        var result = CalendarContextFormatter.Format([allDay], withinDays: 7);

        Assert.Contains("- (2026-08-29, all day) Conference @ Berlin", result);
    }
}
