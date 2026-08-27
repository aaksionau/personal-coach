using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>The assembled context for one coach call: persona + a recent message window + the called coach's own goal/reflection state + the user's values profile + the other coaches' tracked state + the user's upcoming calendar events (no Garmin yet).</summary>
public sealed record CoachContext(
    CoachPersona Persona,
    IReadOnlyList<ChatMessage> RecentMessages,
    CoachTrackedState OwnState,
    ValuesProfile? ValuesProfile,
    IReadOnlyList<CoachTrackedState> OtherCoachStates,
    IReadOnlyList<CalendarEvent> UpcomingEvents);
