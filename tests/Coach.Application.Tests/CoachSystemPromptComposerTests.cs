using Coach.Application.Models;
using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
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
    public async Task Compose_CarriesAnotherCoachsGoalStateIntoThePrompt_SoOneCoachCanReferenceAnother()
    {
        // Build the context the same way a real turn does, then confirm the cross-coach slice
        // survives into the assembled prompt -- the acceptance criterion for issue #8.
        var goalService = new GoalTrackingService(new FakeGoalStore());
        await goalService.CreateGoalAsync("health", "Protect 7 hours of sleep", CancellationToken.None);
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            goalService,
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()));
        var context = await builder.BuildAsync("career", CancellationToken.None);

        var prompt = CoachSystemPromptComposer.Compose(context);

        Assert.Contains("Health Coach:", prompt);
        Assert.Contains("Protect 7 hours of sleep", prompt);
    }

    private static CoachContext ContextFor(CoachPersona persona) =>
        new(persona, [], [], [], ValuesProfile: null, CrossCoachSnapshots: []);
}
