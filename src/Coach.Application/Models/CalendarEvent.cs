namespace Coach.Application.Models;

/// <summary>
/// One upcoming calendar event, normalized away from any provider's shape: a title, a start, an
/// optional end, whether it's an all-day event, and an optional location. Built fresh per turn by
/// the calendar adapter -- never persisted. All-day events carry a date-only <see cref="Start"/>
/// (midnight in the calendar's zone); timed events carry the real instant.
/// </summary>
public sealed record CalendarEvent(
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool IsAllDay,
    string? Location);
