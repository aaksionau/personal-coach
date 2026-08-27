using System.Globalization;
using Coach.Application.Models;
using Google.Apis.Calendar.v3.Data;

namespace Coach.Infrastructure.GoogleCalendar;

/// <summary>
/// Normalizes Google's <see cref="Events"/> list into the provider-agnostic <see cref="CalendarEvent"/>
/// shape: drops cancelled entries and entries with no usable start, distinguishes all-day
/// (date-only) events from timed ones, fills in a placeholder title, and returns them soonest first.
/// All-day dates are anchored to midnight in the calendar's own time zone (<see cref="Events.TimeZone"/>,
/// falling back to UTC) so they render on the right calendar day for a non-UTC user.
/// Pure and network-free so it can be exercised directly in tests.
/// </summary>
internal static class GoogleCalendarEventMapper
{
    public static IReadOnlyList<CalendarEvent> Map(Events response)
    {
        if (response.Items is not { Count: > 0 } items)
        {
            return [];
        }

        var calendarZone = ResolveZone(response.TimeZone);
        var events = new List<CalendarEvent>(items.Count);
        foreach (var item in items)
        {
            if (string.Equals(item.Status, "cancelled", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryResolveStart(item.Start, calendarZone, out var start, out var isAllDay))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(item.Summary) ? "(no title)" : item.Summary.Trim();
            var location = string.IsNullOrWhiteSpace(item.Location) ? null : item.Location.Trim();
            events.Add(new CalendarEvent(title, start, ResolveEnd(item.End, isAllDay, calendarZone), isAllDay, location));
        }

        events.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        return events;
    }

    private static bool TryResolveStart(EventDateTime? when, TimeZoneInfo calendarZone, out DateTimeOffset start, out bool isAllDay)
    {
        start = default;
        isAllDay = false;

        if (when is null)
        {
            return false;
        }

        if (when.DateTimeDateTimeOffset is { } timed)
        {
            start = timed;
            return true;
        }

        if (TryParseAllDayDate(when.Date, calendarZone, out var date))
        {
            start = date;
            isAllDay = true;
            return true;
        }

        return false;
    }

    private static DateTimeOffset? ResolveEnd(EventDateTime? when, bool isAllDay, TimeZoneInfo calendarZone)
    {
        if (when is null)
        {
            return null;
        }

        if (when.DateTimeDateTimeOffset is { } timed)
        {
            return timed;
        }

        return isAllDay && TryParseAllDayDate(when.Date, calendarZone, out var date) ? date : null;
    }

    private static bool TryParseAllDayDate(string? value, TimeZoneInfo calendarZone, out DateTimeOffset date)
    {
        date = default;
        if (string.IsNullOrWhiteSpace(value)
            || !DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return false;
        }

        var midnight = parsed.ToDateTime(TimeOnly.MinValue);
        date = new DateTimeOffset(midnight, calendarZone.GetUtcOffset(midnight));
        return true;
    }

    private static TimeZoneInfo ResolveZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }
}
