using System.Text;
using Coach.Application.Models;

namespace Coach.Application.Formatters;

/// <summary>
/// Formats the user's upcoming calendar events as text for the model's system prompt -- soonest
/// first, each dated, all-day events marked as such. The counterpart to <see cref="GoalContextFormatter"/>
/// and <see cref="ReflectionContextFormatter"/> for the calendar slice of a coach's context, injected
/// identically into all four personas. Read-only: the model is told it cannot act on these.
/// </summary>
public static class CalendarContextFormatter
{
    public static string Format(IReadOnlyList<CalendarEvent> events, int withinDays)
    {
        if (events.Count == 0)
        {
            return $"Upcoming calendar events: none in the next {withinDays} days.";
        }

        var builder = new StringBuilder(
            $"Upcoming calendar events over the next {withinDays} days (read-only -- for scheduling awareness, you cannot change these):\n");
        foreach (var calendarEvent in events)
        {
            builder.Append("- ").Append(FormatWhen(calendarEvent)).Append(' ').Append(calendarEvent.Title);
            if (!string.IsNullOrWhiteSpace(calendarEvent.Location))
            {
                builder.Append(" @ ").Append(calendarEvent.Location);
            }

            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static string FormatWhen(CalendarEvent calendarEvent) =>
        calendarEvent.IsAllDay
            ? $"({calendarEvent.Start:yyyy-MM-dd}, all day)"
            : $"({calendarEvent.Start:yyyy-MM-dd HH:mm})";
}
