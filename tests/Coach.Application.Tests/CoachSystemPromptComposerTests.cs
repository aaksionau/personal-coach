using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class CoachSystemPromptComposerTests
{
    [Fact]
    public void Compose_StartsWithThePersonaInstructions()
    {
        var context = ContextFor(new CoachPersona("career", "Career Coach", "You are the user's career coach.", "direct"));

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.StartsWith("You are the user's career coach.", prompt);
    }

    [Fact]
    public void Compose_CarriesAnotherCoachsState_SoOneCoachCanReferenceAnother()
    {
        var healthGoal = Goal.Create("health", "Protect 7 hours of sleep");
        var context = ContextFor(
            new CoachPersona("career", "Career Coach", "You are the user's career coach.", "direct"),
            otherCoachStates: [new CoachTrackedState("Health Coach", [new GoalWithActionItems(healthGoal, [])], [])]);

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.Contains("Health Coach:", prompt);
        Assert.Contains("Protect 7 hours of sleep", prompt);
    }

    [Fact]
    public void Compose_CarriesUpcomingCalendarEvents_SoTheCoachCanAccountForTheSchedule()
    {
        var context = ContextFor(
            new CoachPersona("career", "Career Coach", "You are the user's career coach.", "direct"),
            upcomingEvents: [new CalendarEvent("On-site interview", new DateTimeOffset(2026, 9, 2, 14, 0, 0, TimeSpan.Zero), null, IsAllDay: false, Location: null)]);

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.Contains("On-site interview", prompt);
    }

    [Fact]
    public void Compose_CarriesGarminMetrics_ForTheHealthCoach()
    {
        var context = ContextFor(
            new CoachPersona("health", "Health Coach", "You are the user's health coach.", "calm", IncludesGarminMetrics: true),
            garminMetrics: new GarminMetricsSnapshot(
                new GarminDailyMetric { Date = new DateOnly(2026, 8, 26), Steps = 12345, IngestedAtUtc = DateTimeOffset.UtcNow },
                []));

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.Contains("Latest Garmin metrics", prompt);
        Assert.Contains("12345", prompt);
    }

    [Fact]
    public void Compose_OmitsAnyGarminSlice_ForANonHealthCoach()
    {
        var context = ContextFor(
            new CoachPersona("career", "Career Coach", "You are the user's career coach.", "direct"),
            garminMetrics: new GarminMetricsSnapshot(
                new GarminDailyMetric { Date = new DateOnly(2026, 8, 26), IngestedAtUtc = DateTimeOffset.UtcNow }, []));

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.DoesNotContain("Garmin", prompt);
    }

    private static CoachContext ContextFor(
        CoachPersona persona,
        IReadOnlyList<CoachTrackedState>? otherCoachStates = null,
        IReadOnlyList<CalendarEvent>? upcomingEvents = null,
        GarminMetricsSnapshot? garminMetrics = null) =>
        new(persona, [], new CoachTrackedState(persona.Name, [], []), ValuesProfile: null, otherCoachStates ?? [], upcomingEvents ?? [], garminMetrics);
}
