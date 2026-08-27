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

    private static CoachContext ContextFor(
        CoachPersona persona, IReadOnlyList<CoachTrackedState>? otherCoachStates = null) =>
        new(persona, [], new CoachTrackedState(persona.Name, [], []), ValuesProfile: null, otherCoachStates ?? []);
}
