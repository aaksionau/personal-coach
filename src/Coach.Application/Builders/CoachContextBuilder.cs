using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Application.Services;

namespace Coach.Application.Builders;

/// <summary>
/// Assembles the context bundle for a coach call. v1 includes the persona, a recent message
/// window, the called coach's own goal/reflection state, the user's global values profile, the
/// other coaches' tracked state (so one coach can account for the other domains), the user's
/// upcoming calendar events, and -- for the Health coach only -- the latest ingested Garmin
/// daily metrics. Deliberately separate from the Conversation Engine so it's testable without a
/// model call.
/// </summary>
public sealed class CoachContextBuilder(
    IChatMessageStore chatMessageStore,
    CoachPersonaRegistry personaRegistry,
    GoalTrackingService goalTrackingService,
    ReflectionService reflectionService,
    ValuesProfileService valuesProfileService,
    ICalendarReader calendarReader,
    IGarminMetricsStore garminMetricsStore)
{
    private const int RecentMessageWindow = 20;
    private const int OwnReflectionWindow = 10;

    /// <summary>Reflections per other coach -- a terser window than a coach's own, since this is background awareness, not the working record.</summary>
    private const int OtherCoachReflectionWindow = 3;

    public async Task<CoachContext> BuildAsync(string coachSlug, CancellationToken cancellationToken)
    {
        var persona = personaRegistry.Get(coachSlug);

        var recentMessagesTask = chatMessageStore.GetRecentAsync(coachSlug, RecentMessageWindow, cancellationToken);
        var valuesProfileTask = valuesProfileService.GetProfileAsync(cancellationToken);
        var ownStateTask = BuildTrackedStateAsync(persona, OwnReflectionWindow, cancellationToken);
        var otherStatesTask = BuildOtherCoachStatesAsync(coachSlug, cancellationToken);
        var upcomingEventsTask = calendarReader.GetUpcomingEventsAsync(CoachContext.CalendarLookaheadDays, cancellationToken);
        // Only the Health coach sees Garmin data -- don't touch the table for the other three.
        var garminMetricsTask = persona.IncludesGarminMetrics
            ? garminMetricsStore.GetLatestAsync(cancellationToken)
            : Task.FromResult<GarminMetricsSnapshot?>(null);
        await Task.WhenAll(
            recentMessagesTask, valuesProfileTask, ownStateTask, otherStatesTask, upcomingEventsTask, garminMetricsTask);

        return new CoachContext(
            persona,
            recentMessagesTask.Result,
            ownStateTask.Result,
            valuesProfileTask.Result,
            otherStatesTask.Result,
            upcomingEventsTask.Result,
            garminMetricsTask.Result);
    }

    /// <summary>
    /// Every registered coach's tracked state (goals + a recent reflection window). Feeds the weekly
    /// digest, which summarises across all four coaches at once rather than from any single coach's
    /// point of view. Read fresh, like the per-turn context.
    /// </summary>
    public Task<IReadOnlyList<CoachTrackedState>> BuildAllTrackedStatesAsync(CancellationToken cancellationToken) =>
        BuildStatesAsync(personaRegistry.GetAll(), OwnReflectionWindow, cancellationToken);

    /// <summary>
    /// The other registered personas' tracked state. Read fresh here rather than cached, so a coach
    /// always sees the other domains' current state.
    /// </summary>
    private Task<IReadOnlyList<CoachTrackedState>> BuildOtherCoachStatesAsync(
        string coachSlug, CancellationToken cancellationToken) =>
        BuildStatesAsync(
            personaRegistry.GetAll().Where(p => !string.Equals(p.Slug, coachSlug, StringComparison.Ordinal)),
            OtherCoachReflectionWindow,
            cancellationToken);

    /// <summary>
    /// Fans <see cref="BuildTrackedStateAsync"/> out across <paramref name="personas"/>, ordered by
    /// coach name so the assembled context is deterministic.
    /// </summary>
    private async Task<IReadOnlyList<CoachTrackedState>> BuildStatesAsync(
        IEnumerable<CoachPersona> personas, int reflectionWindow, CancellationToken cancellationToken) =>
        await Task.WhenAll(personas
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => BuildTrackedStateAsync(p, reflectionWindow, cancellationToken)));

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
