using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>The assembled context for one coach call: persona + a recent message window + current goals/action items (no values/calendar/Garmin yet).</summary>
public sealed record CoachContext(CoachPersona Persona, IReadOnlyList<ChatMessage> RecentMessages, IReadOnlyList<GoalWithActionItems> Goals);
