using Coach.Infrastructure.GoogleCalendar;
using Coach.Infrastructure.GoogleCalendar.Tests.Fakes;
using Google.Apis.Calendar.v3.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.GoogleCalendar.Tests;

public class GoogleCalendarReaderTests
{
    private static readonly GoogleCalendarOptions Configured = new()
    {
        ClientId = "client-id",
        ClientSecret = "client-secret",
        RefreshToken = "refresh-token",
    };

    [Fact]
    public async Task GetUpcomingEventsAsync_QueriesAWindowOfTheRequestedLength()
    {
        var events = new FakeGoogleCalendarEvents();
        var reader = ReaderFor(events, Configured);

        await reader.GetUpcomingEventsAsync(7, CancellationToken.None);

        Assert.True(events.WasCalled);
        Assert.Equal(TimeSpan.FromDays(7), events.LastTimeMax - events.LastTimeMin);
        Assert.True(events.LastTimeMin <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsNormalizedEvents_FromTheApi()
    {
        var start = new DateTimeOffset(2026, 8, 28, 9, 0, 0, TimeSpan.Zero);
        var events = new FakeGoogleCalendarEvents
        {
            Response = new Events
            {
                Items = [new Event { Summary = "Standup", Start = new EventDateTime { DateTimeDateTimeOffset = start } }],
            },
        };

        var result = await ReaderFor(events, Configured).GetUpcomingEventsAsync(7, CancellationToken.None);

        Assert.Equal("Standup", Assert.Single(result).Title);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsEmpty_AndSkipsTheApi_WhenNotConfigured()
    {
        var events = new FakeGoogleCalendarEvents();

        var result = await ReaderFor(events, new GoogleCalendarOptions()).GetUpcomingEventsAsync(7, CancellationToken.None);

        Assert.Empty(result);
        Assert.False(events.WasCalled);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_ReturnsEmpty_WhenTheApiThrows()
    {
        var events = new FakeGoogleCalendarEvents { ThrowOnList = new InvalidOperationException("google is down") };

        var result = await ReaderFor(events, Configured).GetUpcomingEventsAsync(7, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUpcomingEventsAsync_PropagatesCancellation()
    {
        var events = new FakeGoogleCalendarEvents { ThrowOnList = new OperationCanceledException() };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => ReaderFor(events, Configured).GetUpcomingEventsAsync(7, CancellationToken.None));
    }

    private static GoogleCalendarReader ReaderFor(IGoogleCalendarEvents events, GoogleCalendarOptions options) =>
        new(events, Options.Create(options), NullLogger<GoogleCalendarReader>.Instance);
}
