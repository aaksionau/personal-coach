using Coach.Application.Interfaces;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>
/// Assembles the context bundle for a coach call. v1 includes the persona, a recent message
/// window, the called coach's own goal/reflection state, the user's global values profile, and the
/// other coaches' tracked state (so one coach can account for the other domains) -- no calendar or
/// Garmin data yet. Deliberately separate from the Conversation Engine so it's testable without a
/// model call.
/// </summary>
public sealed class CoachContextBuilder(
    IChatMessageStore chatMessageStore,
    CoachPersonaRegistry personaRegistry,
    GoalTrackingService goalTrackingService,
    ReflectionService reflectionService,
    ValuesProfileService valuesProfileService)
{
    private const int RecentMessageWindow = 20;
    private const int OwnReflectionWindow = 10;

    /// <summary>Reflections per other coach -- a terser window than a coach's own, since this is background awareness, not the working record.</summary>
    private const int OtherCoachReflectionWindow = 3;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        if (!personaRegistry.TryGet(coachSlug, out var persona))
        {
            throw new ArgumentException($"No coach persona registered for slug '{coachSlug}'.", nameof(coachSlug));
        }

        var recentMessagesTask = chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        var valuesProfileTask = valuesProfileService.GetProfileAsync(cancellationToken);
        var ownStateTask = BuildTrackedStateAsync(persona, OwnReflectionWindow, cancellationToken);
        var otherStatesTask = BuildOtherCoachStatesAsync(coachSlug, cancellationToken);
        await Task.WhenAll(recentMessagesTask, valuesProfileTask, ownStateTask, otherStatesTask);

        return new CoachContext(
            persona,
            recentMessagesTask.Result,
            ownStateTask.Result,
            valuesProfileTask.Result,
            otherStatesTask.Result);
    }

    /// <summary>
    /// The other registered personas' tracked state, ordered by coach name so the assembled context
    /// is deterministic. Read fresh here rather than cached, so a coach always sees the other
    /// domains' current state.
    /// </summary>
    private async Task<IReadOnlyList<CoachTrackedState>> BuildOtherCoachStatesAsync(
        string coachSlug, CancellationToken cancellationToken)
    {
        var otherPersonas = personaRegistry.GetAll()
            .Where(p => !string.Equals(p.Slug, coachSlug, StringComparison.Ordinal))
            .OrderBy(p => p.Name, StringComparer.Ordinal);

        return await Task.WhenAll(
            otherPersonas.Select(p => BuildTrackedStateAsync(p, OtherCoachReflectionWindow, cancellationToken)));
    }

    /// <summary>One coach's goals and recent reflections -- the single seam both the own-coach and cross-coach paths read through.</summary>
    private async Task<CoachTrackedState> BuildTrackedStateAsync(
        CoachPersona persona, int reflectionWindow, CancellationToken cancellationToken)
    {
        var goalsTask = goalTrackingService.GetGoalsAsync(persona.Slug, cancellationToken);
        var reflectionsTask = reflectionService.GetRecentReflectionsAsync(persona.Slug, reflectionWindow, cancellationToken);
        await Task.WhenAll(goalsTask, reflectionsTask);

        return new CoachTrackedState(persona.Name, goalsTask.Result, reflectionsTask.Result);
    }
}
