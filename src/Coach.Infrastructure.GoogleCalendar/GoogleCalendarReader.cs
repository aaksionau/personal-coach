using Coach.Application.Interfaces;
using Coach.Application.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.GoogleCalendar;

/// <summary>
/// <see cref="ICalendarReader"/> backed by the Google Calendar API. Turns a "within N days" request
/// into a concrete UTC time window, fetches it through <see cref="IGoogleCalendarEvents"/>, and
/// normalizes the result via <see cref="GoogleCalendarEventMapper"/>. Returns an empty list -- never
/// throws -- when the calendar isn't configured or the API call fails, so a coach turn is never
/// blocked by calendar trouble.
/// </summary>
internal sealed class GoogleCalendarReader(
    IGoogleCalendarEvents events,
    IOptions<GoogleCalendarOptions> options,
    ILogger<GoogleCalendarReader> logger) : ICalendarReader
{
    public async Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int withinDays, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            logger.LogDebug("Google Calendar is not configured; returning no upcoming events.");
            return [];
        }

        var timeMin = DateTimeOffset.UtcNow;
        var timeMax = timeMin.AddDays(Math.Max(withinDays, 0));

        try
        {
            var response = await events.ListAsync(timeMin, timeMax, cancellationToken);
            return GoogleCalendarEventMapper.Map(response);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to read upcoming events from Google Calendar; continuing without them.");
            return [];
        }
    }
}
