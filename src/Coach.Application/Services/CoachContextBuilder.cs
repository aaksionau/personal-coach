using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>
/// Assembles the context bundle for a coach call. v1 includes the persona, a recent message
/// window, and the coach's own goal/action-item state -- no values, calendar, cross-coach
/// summary, or Garmin data yet. Deliberately separate from the Conversation Engine so it's
/// testable without a model call.
/// </summary>
public sealed class CoachContextBuilder(
    IChatMessageStore chatMessageStore, CoachPersonaRegistry personaRegistry, GoalTrackingService goalTrackingService)
{
    private const int RecentMessageWindow = 20;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        if (!personaRegistry.TryGet(coachSlug, out var persona))
        {
            throw new ArgumentException($"No coach persona registered for slug '{coachSlug}'.", nameof(coachSlug));
        }

        var recentMessagesTask = chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        var goalsTask = goalTrackingService.GetGoalsAsync(coachSlug, cancellationToken);
        await Task.WhenAll(recentMessagesTask, goalsTask);

        return new CoachContext(persona, recentMessagesTask.Result, goalsTask.Result);
    }
}
