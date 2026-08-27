using Coach.Application.Models;

namespace Coach.Application.Interfaces;

/// <summary>
/// Read-only view of the user's upcoming calendar, hiding all provider/OAuth detail from callers.
/// Fetched live at the point of use (a coach turn), never synced into the database.
/// </summary>
public interface ICalendarReader
{
    /// <summary>
    /// Events starting within the next <paramref name="withinDays"/> days, soonest first.
    /// Returns an empty list when the calendar isn't connected or is unreachable -- a missing
    /// calendar must never break a coach turn, so this method does not throw for those cases.
    /// </summary>
    Task<IReadOnlyList<CalendarEvent>> GetUpcomingEventsAsync(int withinDays, CancellationToken cancellationToken);
}
