using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>The assembled context for one coach call: persona + a recent message window (no goals/values/calendar/Garmin yet).</summary>
public sealed record CoachContext(CoachPersona Persona, IReadOnlyList<ChatMessage> RecentMessages);
