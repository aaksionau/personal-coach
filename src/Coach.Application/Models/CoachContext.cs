using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>The assembled context for one coach call: persona + a recent message window + the called coach's own goal/reflection state + the user's values profile + the other coaches' tracked state + the user's upcoming calendar events + -- Health coach only -- the latest Garmin daily metrics.</summary>
public sealed record CoachContext(
    CoachPersona Persona,
    IReadOnlyList<ChatMessage> RecentMessages,
    CoachTrackedState OwnState,
    ValuesProfile? ValuesProfile,
    IReadOnlyList<CoachTrackedState> OtherCoachStates,
    IReadOnlyList<CalendarEvent> UpcomingEvents,
    GarminMetricsSnapshot? GarminMetrics)
{
    /// <summary>How far ahead a coach's context looks for calendar events -- the single source shared
    /// by the builder's fetch window and the formatter's "next N days" prose so the two can't drift.</summary>
    public const int CalendarLookaheadDays = 7;
}
