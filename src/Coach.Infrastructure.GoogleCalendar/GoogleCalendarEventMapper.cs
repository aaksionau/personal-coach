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

            if (!TryResolve(item.Start, calendarZone, out var start, out var isAllDay))
            {
                continue;
            }

            var title = string.IsNullOrWhiteSpace(item.Summary) ? "(no title)" : item.Summary.Trim();
            var location = string.IsNullOrWhiteSpace(item.Location) ? null : item.Location.Trim();
            var end = TryResolve(item.End, calendarZone, out var endValue, out _) ? endValue : (DateTimeOffset?)null;
            events.Add(new CalendarEvent(title, start, end, isAllDay, location));
        }

        events.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        return events;
    }

    /// <summary>
    /// Resolves one <see cref="EventDateTime"/> endpoint: a timed instant when Google gives one, else
    /// the date-only all-day value anchored to midnight in the calendar's zone. False when neither is
    /// present. Used for both the start (which must resolve) and the optional end.
    /// </summary>
    private static bool TryResolve(EventDateTime? when, TimeZoneInfo calendarZone, out DateTimeOffset value, out bool isAllDay)
    {
        value = default;
        isAllDay = false;

        if (when is null)
        {
            return false;
        }

        if (when.DateTimeDateTimeOffset is { } timed)
        {
            value = timed;
            return true;
        }

        if (TryParseAllDayDate(when.Date, calendarZone, out var date))
        {
            value = date;
            isAllDay = true;
            return true;
        }

        return false;
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
