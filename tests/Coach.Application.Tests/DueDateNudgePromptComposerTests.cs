using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Application.Services;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class DueDateNudgePromptComposerTests
{
    private static readonly CoachPersonaRegistry Registry = new();

    private static CoachPersona Career()
    {
        Registry.TryGet("career", out var persona);
        return persona!;
    }

    [Fact]
    public void Compose_GroundsTheMessageInTheSpecificGoalAndActionItem()
    {
        var goal = Goal.Create("career", "Land a staff role");
        var item = ActionItem.Create(goal.Id, "Send the recruiter my updated resume", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(
            Career(), "Land a staff role", item, new DateOnly(2026, 8, 30), valuesProfile: null, priorNudgeCount: 0);

        Assert.Contains("Land a staff role", prompt);
        Assert.Contains("Send the recruiter my updated resume", prompt);
        Assert.Contains("2026-09-01", prompt);
    }

    [Fact]
    public void Compose_CarriesThePersonaVoice()
    {
        var goal = Goal.Create("career", "x");
        var item = ActionItem.Create(goal.Id, "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(
            Career(), "x", item, new DateOnly(2026, 8, 30), null, 0);

        Assert.Contains(Career().SystemPrompt, prompt);
        Assert.Contains(Career().Tone, prompt);
    }

    [Fact]
    public void Compose_TellsTheModelToStayOnOneItemAndNotAskForATextReply()
    {
        var goal = Goal.Create("career", "x");
        var item = ActionItem.Create(goal.Id, "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), "x", item, new DateOnly(2026, 8, 30), null, 0);

        Assert.Contains("ONE specific action item", prompt);
        Assert.Contains("not the weekly", prompt);
        Assert.Contains("opening the app", prompt);
    }

    [Fact]
    public void Compose_DescribesHowCloseTheDueDateIs()
    {
        var goal = Goal.Create("career", "x");
        var tomorrow = new DateOnly(2026, 8, 31);
        var item = ActionItem.Create(goal.Id, "x", tomorrow);

        var prompt = DueDateNudgePromptComposer.Compose(Career(), "x", item, new DateOnly(2026, 8, 30), null, 0);

        Assert.Contains("tomorrow", prompt);
    }

    [Fact]
    public void Compose_FlagsAnOverdueItem()
    {
        var goal = Goal.Create("career", "x");
        var item = ActionItem.Create(goal.Id, "x", new DateOnly(2026, 8, 28));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), "x", item, new DateOnly(2026, 8, 30), null, 0);

        Assert.Contains("2 days overdue", prompt);
    }

    [Fact]
    public void Compose_ReflectsTheEscalationContextWhenTheItemHasBeenNudgedBefore()
    {
        var goal = Goal.Create("career", "x");
        var item = ActionItem.Create(goal.Id, "x", new DateOnly(2026, 9, 1));

        var first = DueDateNudgePromptComposer.Compose(Career(), "x", item, new DateOnly(2026, 8, 30), null, 0);
        var repeat = DueDateNudgePromptComposer.Compose(Career(), "x", item, new DateOnly(2026, 8, 30), null, 3);

        Assert.Contains("first nudge", first);
        Assert.Contains("nudged 3 times before", repeat);
    }

    [Fact]
    public void Compose_IncludesTheValuesProfileWhenSet()
    {
        var goal = Goal.Create("career", "x");
        var item = ActionItem.Create(goal.Id, "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(
            Career(), "x", item, new DateOnly(2026, 8, 30), ValuesProfile.Create("Family evenings are protected."), 0);

        Assert.Contains("Family evenings are protected.", prompt);
    }
}
