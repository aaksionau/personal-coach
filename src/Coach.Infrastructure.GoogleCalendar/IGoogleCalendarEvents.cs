using Google.Apis.Calendar.v3.Data;

namespace Coach.Infrastructure.GoogleCalendar;

/// <summary>
/// The seam between <see cref="GoogleCalendarReader"/> and the live Google Calendar API: hands back
/// the raw <see cref="Events"/> list for a time window. Split out so the reader's date-range logic
/// and <see cref="GoogleCalendarEventMapper"/>'s normalization can be tested against a fake with no
/// network. Read-only by construction -- there is deliberately no write method here.
/// </summary>
internal interface IGoogleCalendarEvents
{
    Task<Events> ListAsync(DateTimeOffset timeMin, DateTimeOffset timeMax, CancellationToken cancellationToken);
}
