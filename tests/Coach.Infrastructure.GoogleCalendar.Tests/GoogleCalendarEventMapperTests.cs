using Coach.Infrastructure.GoogleCalendar;
using Google.Apis.Calendar.v3.Data;

namespace Coach.Infrastructure.GoogleCalendar.Tests;

public class GoogleCalendarEventMapperTests
{
    [Fact]
    public void Map_NormalizesATimedEvent()
    {
        var start = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        var end = start.AddMinutes(30);
        var response = ResponseWith(new Event
        {
            Summary = "Standup",
            Location = "Room 4",
            Status = "confirmed",
            Start = new EventDateTime { DateTimeDateTimeOffset = start },
            End = new EventDateTime { DateTimeDateTimeOffset = end },
        });

        var result = GoogleCalendarEventMapper.Map(response);

        var mapped = Assert.Single(result);
        Assert.Equal("Standup", mapped.Title);
        Assert.Equal("Room 4", mapped.Location);
        Assert.False(mapped.IsAllDay);
        Assert.Equal(start, mapped.Start);
        Assert.Equal(end, mapped.End);
    }

    [Fact]
    public void Map_NormalizesAnAllDayEvent_FromTheDateField()
    {
        var response = ResponseWith(new Event
        {
            Summary = "Conference",
            Start = new EventDateTime { Date = "2026-08-29" },
            End = new EventDateTime { Date = "2026-08-31" },
        });

        var mapped = Assert.Single(GoogleCalendarEventMapper.Map(response));

        Assert.True(mapped.IsAllDay);
        Assert.Equal(new DateTimeOffset(2026, 8, 29, 0, 0, 0, TimeSpan.Zero), mapped.Start);
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 0, 0, 0, TimeSpan.Zero), mapped.End);
    }

    [Fact]
    public void Map_SkipsCancelledEvents()
    {
        var response = ResponseWith(new Event
        {
            Summary = "Cancelled sync",
            Status = "cancelled",
            Start = new EventDateTime { DateTimeDateTimeOffset = DateTimeOffset.UtcNow },
        });

        Assert.Empty(GoogleCalendarEventMapper.Map(response));
    }

    [Fact]
    public void Map_SkipsEventsWithNoUsableStart()
    {
        var response = ResponseWith(new Event { Summary = "Floating", Start = null });

        Assert.Empty(GoogleCalendarEventMapper.Map(response));
    }

    [Fact]
    public void Map_FallsBackToAPlaceholderTitle_AndToleratesAMissingEnd()
    {
        var response = ResponseWith(new Event
        {
            Summary = "   ",
            Start = new EventDateTime { DateTimeDateTimeOffset = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero) },
            End = null,
        });

        var mapped = Assert.Single(GoogleCalendarEventMapper.Map(response));

        Assert.Equal("(no title)", mapped.Title);
        Assert.Null(mapped.End);
        Assert.Null(mapped.Location);
    }

    [Fact]
    public void Map_ReturnsEventsSoonestFirst()
    {
        var later = new DateTimeOffset(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);
        var earlier = new DateTimeOffset(2026, 8, 28, 8, 0, 0, TimeSpan.Zero);
        var response = ResponseWith(
            new Event { Summary = "Later", Start = new EventDateTime { DateTimeDateTimeOffset = later } },
            new Event { Summary = "Earlier", Start = new EventDateTime { DateTimeDateTimeOffset = earlier } });

        var result = GoogleCalendarEventMapper.Map(response);

        Assert.Equal(["Earlier", "Later"], result.Select(e => e.Title));
    }

    [Fact]
    public void Map_ReturnsEmpty_WhenThereAreNoItems()
    {
        Assert.Empty(GoogleCalendarEventMapper.Map(new Events { Items = null }));
    }

    private static Events ResponseWith(params Event[] items) => new() { Items = items };
}
