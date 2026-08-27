using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class WeeklyDigestPromptComposerTests
{
    private static CoachTrackedState State(string coachName, string? goalTitle = null, string? reflection = null) =>
        new(
            coachName,
            goalTitle is null ? [] : [new GoalWithActionItems(Goal.Create(coachName, goalTitle), [])],
            reflection is null ? [] : [Reflection.Create(coachName, reflection)]);

    [Fact]
    public void Compose_IncludesEveryCoachThatHasTrackedState()
    {
        var prompt = WeeklyDigestPromptComposer.Compose(
            [
                State("Career Coach", goalTitle: "Land a staff role"),
                State("Health Coach", reflection: "Sleeping better on weeknights."),
                State("Relationships Coach", goalTitle: "Weekly date night"),
                State("Kids Coach", goalTitle: "Read together nightly"),
            ],
            [],
            valuesProfile: null);

        Assert.Contains("Career Coach", prompt);
        Assert.Contains("Land a staff role", prompt);
        Assert.Contains("Health Coach", prompt);
        Assert.Contains("Sleeping better on weeknights.", prompt);
        Assert.Contains("Relationships Coach", prompt);
        Assert.Contains("Kids Coach", prompt);
    }

    [Fact]
    public void Compose_TellsTheModelToWriteOneCrossCoachTextAndNotAskForAReply()
    {
        var prompt = WeeklyDigestPromptComposer.Compose([State("Career Coach", goalTitle: "x")], [], null);

        Assert.Contains("one message", prompt);
        Assert.Contains("open", prompt); // reply by opening the app, not by text
    }

    [Fact]
    public void Compose_SkipsCoachesWithNothingTracked()
    {
        var prompt = WeeklyDigestPromptComposer.Compose(
            [State("Career Coach", goalTitle: "Land a staff role"), State("Kids Coach")],
            [],
            null);

        Assert.DoesNotContain("Kids Coach", prompt);
    }

    [Fact]
    public void Compose_StatesPlainlyWhenNothingIsTrackedAtAll()
    {
        var prompt = WeeklyDigestPromptComposer.Compose([State("Career Coach"), State("Kids Coach")], [], null);

        Assert.Contains("nothing is being tracked yet", prompt);
    }

    [Fact]
    public void Compose_IncludesUpcomingCalendarEvents()
    {
        var prompt = WeeklyDigestPromptComposer.Compose(
            [State("Career Coach", goalTitle: "x")],
            [new CalendarEvent("Performance review", DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddHours(1), IsAllDay: false, Location: null)],
            null);

        Assert.Contains("Performance review", prompt);
    }

    [Fact]
    public void Compose_IncludesTheValuesProfileWhenSet()
    {
        var prompt = WeeklyDigestPromptComposer.Compose(
            [State("Career Coach", goalTitle: "x")],
            [],
            ValuesProfile.Create("Family evenings are protected."));

        Assert.Contains("Family evenings are protected.", prompt);
    }
}
