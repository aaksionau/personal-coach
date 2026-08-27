using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>
/// Assembles the context bundle for a coach call. v1 includes the persona, a recent message
/// window, the coach's own goal/action-item state, and its recent reflections -- no values,
/// calendar, cross-coach summary, or Garmin data yet. Deliberately separate from the Conversation
/// Engine so it's testable without a model call.
/// </summary>
public sealed class CoachContextBuilder(
    IChatMessageStore chatMessageStore,
    CoachPersonaRegistry personaRegistry,
    GoalTrackingService goalTrackingService,
    ReflectionService reflectionService)
{
    private const int RecentMessageWindow = 20;
    private const int RecentReflectionWindow = 10;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        if (!personaRegistry.TryGet(coachSlug, out var persona))
        {
            throw new ArgumentException($"No coach persona registered for slug '{coachSlug}'.", nameof(coachSlug));
        }

        var recentMessagesTask = chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        var goalsTask = goalTrackingService.GetGoalsAsync(coachSlug, cancellationToken);
        var reflectionsTask = reflectionService.GetRecentReflectionsAsync(coachSlug, RecentReflectionWindow, cancellationToken);
        await Task.WhenAll(recentMessagesTask, goalsTask, reflectionsTask);

        return new CoachContext(persona, recentMessagesTask.Result, goalsTask.Result, reflectionsTask.Result);
    }
}
