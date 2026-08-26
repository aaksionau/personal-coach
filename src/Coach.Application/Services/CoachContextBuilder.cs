using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>
/// Assembles the context bundle for a coach call. v1 only includes the persona and a recent
/// message window -- no goals, values, calendar, cross-coach summary, or Garmin data yet.
/// Deliberately separate from the Conversation Engine so it's testable without a model call.
/// </summary>
public sealed class CoachContextBuilder(IChatMessageStore chatMessageStore, CoachPersonaRegistry personaRegistry)
{
    private const int RecentMessageWindow = 20;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        if (!personaRegistry.TryGet(coachSlug, out var persona))
        {
            throw new ArgumentException($"No coach persona registered for slug '{coachSlug}'.", nameof(coachSlug));
        }

        var recentMessages = await chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        return new CoachContext(persona, recentMessages);
    }
}
