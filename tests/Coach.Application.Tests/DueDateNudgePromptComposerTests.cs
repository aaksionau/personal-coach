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

    private static PendingNudge Pending(
        string goalTitle, string description, DateOnly dueDate, int priorNudgeCount = 0)
    {
        var goal = Goal.Create("career", goalTitle);
        var item = ActionItem.Create(goal.Id, description, dueDate);
        return new PendingNudge("career", goalTitle, item, priorNudgeCount);
    }

    [Fact]
    public void Compose_GroundsTheMessageInTheSpecificGoalAndActionItem()
    {
        var pending = Pending("Land a staff role", "Send the recruiter my updated resume", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(
            Career(), pending, new DateOnly(2026, 8, 30), valuesProfile: null);

        Assert.Contains("Land a staff role", prompt);
        Assert.Contains("Send the recruiter my updated resume", prompt);
        Assert.Contains("2026-09-01", prompt);
    }

    [Fact]
    public void Compose_CarriesThePersonaVoice()
    {
        var pending = Pending("x", "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), pending, new DateOnly(2026, 8, 30), null);

        Assert.Contains(Career().SystemPrompt, prompt);
        Assert.Contains(Career().Tone, prompt);
    }

    [Fact]
    public void Compose_TellsTheModelToStayOnOneItemAndNotAskForATextReply()
    {
        var pending = Pending("x", "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), pending, new DateOnly(2026, 8, 30), null);

        Assert.Contains("ONE specific action item", prompt);
        Assert.Contains("not the weekly", prompt);
        Assert.Contains("opening the app", prompt);
    }

    [Fact]
    public void Compose_DescribesHowCloseTheDueDateIs()
    {
        var pending = Pending("x", "x", new DateOnly(2026, 8, 31));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), pending, new DateOnly(2026, 8, 30), null);

        Assert.Contains("tomorrow", prompt);
    }

    [Fact]
    public void Compose_FlagsAnOverdueItem()
    {
        var pending = Pending("x", "x", new DateOnly(2026, 8, 28));

        var prompt = DueDateNudgePromptComposer.Compose(Career(), pending, new DateOnly(2026, 8, 30), null);

        Assert.Contains("2 days overdue", prompt);
    }

    [Fact]
    public void Compose_ReflectsTheEscalationContextWhenTheItemHasBeenNudgedBefore()
    {
        var first = DueDateNudgePromptComposer.Compose(
            Career(), Pending("x", "x", new DateOnly(2026, 9, 1)), new DateOnly(2026, 8, 30), null);
        var repeat = DueDateNudgePromptComposer.Compose(
            Career(), Pending("x", "x", new DateOnly(2026, 9, 1), priorNudgeCount: 3), new DateOnly(2026, 8, 30), null);

        Assert.Contains("first nudge", first);
        Assert.Contains("nudged 3 times before", repeat);
    }

    [Fact]
    public void Compose_IncludesTheValuesProfileWhenSet()
    {
        var pending = Pending("x", "x", new DateOnly(2026, 9, 1));

        var prompt = DueDateNudgePromptComposer.Compose(
            Career(), pending, new DateOnly(2026, 8, 30), ValuesProfile.Create("Family evenings are protected."));

        Assert.Contains("Family evenings are protected.", prompt);
    }
}
