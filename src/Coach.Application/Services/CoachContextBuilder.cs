using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>
/// Assembles the context bundle for a coach call. v1 includes the persona, a recent message
/// window, the coach's own goal/action-item state, its recent reflections, the user's global
/// values profile, and a cross-coach snapshot of the other personas' current goals/reflections --
/// no calendar or Garmin data yet. Deliberately separate from the Conversation Engine so it's
/// testable without a model call.
/// </summary>
public sealed class CoachContextBuilder(
    IChatMessageStore chatMessageStore,
    CoachPersonaRegistry personaRegistry,
    GoalTrackingService goalTrackingService,
    ReflectionService reflectionService,
    ValuesProfileService valuesProfileService)
{
    private const int RecentMessageWindow = 20;
    private const int RecentReflectionWindow = 10;

    /// <summary>Reflections per other coach in the cross-coach snapshot -- a terser window than a coach's own, since this is background awareness, not the working record.</summary>
    private const int CrossCoachReflectionWindow = 3;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        if (!personaRegistry.TryGet(coachSlug, out var persona))
        {
            throw new ArgumentException($"No coach persona registered for slug '{coachSlug}'.", nameof(coachSlug));
        }

        var recentMessagesTask = chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        var goalsTask = goalTrackingService.GetGoalsAsync(coachSlug, cancellationToken);
        var reflectionsTask = reflectionService.GetRecentReflectionsAsync(coachSlug, RecentReflectionWindow, cancellationToken);
        var valuesProfileTask = valuesProfileService.GetProfileAsync(cancellationToken);
        var crossCoachTask = BuildCrossCoachSnapshotsAsync(coachSlug, cancellationToken);
        await Task.WhenAll(recentMessagesTask, goalsTask, reflectionsTask, valuesProfileTask, crossCoachTask);

        return new CoachContext(
            persona,
            recentMessagesTask.Result,
            goalsTask.Result,
            reflectionsTask.Result,
            valuesProfileTask.Result,
            crossCoachTask.Result);
    }

    /// <summary>
    /// One snapshot per registered persona other than <paramref name="coachSlug"/>, ordered by coach
    /// name so the assembled context is deterministic. Read fresh here rather than cached, so a coach
    /// always sees the other domains' current state.
    /// </summary>
    private async Task<IReadOnlyList<CrossCoachSnapshot>> BuildCrossCoachSnapshotsAsync(
        string coachSlug, CancellationToken cancellationToken)
    {
        var otherPersonas = personaRegistry.GetAll()
            .Where(p => !string.Equals(p.Slug, coachSlug, StringComparison.Ordinal))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToList();

        var snapshotTasks = otherPersonas.Select(async persona => new CrossCoachSnapshot(
            persona.Name,
            await goalTrackingService.GetGoalsAsync(persona.Slug, cancellationToken),
            await reflectionService.GetRecentReflectionsAsync(persona.Slug, CrossCoachReflectionWindow, cancellationToken)));

        return await Task.WhenAll(snapshotTasks);
    }
}
