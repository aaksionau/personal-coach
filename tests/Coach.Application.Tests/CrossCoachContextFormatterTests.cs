using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Tests;

public class CrossCoachContextFormatterTests
{
    [Fact]
    public void Format_ReturnsAPlaceholder_WhenNoOtherCoachHasAnything()
    {
        var otherCoachStates = new[]
        {
            new CoachTrackedState("Health Coach", [], []),
            new CoachTrackedState("Kids Coach", [], []),
        };

        var result = CrossCoachContextFormatter.Format(otherCoachStates);

        Assert.Equal("The user's other coaches: nothing tracked yet.", result);
    }

    [Fact]
    public void Format_SummarisesEachOtherCoachsGoalsActionItemsAndReflections_WithoutIds()
    {
        var healthGoal = Goal.Create("health", "Sleep 8 hours");
        var openItem = ActionItem.Create(healthGoal.Id, "No screens after 22:00", new DateOnly(2026, 9, 1));
        var doneItem = ActionItem.Create(healthGoal.Id, "Buy blackout curtains", null);
        doneItem.Status = ActionItemStatus.Done;
        var healthReflection = new Reflection
        {
            Id = Guid.NewGuid(),
            CoachSlug = "health",
            Content = "I skip workouts when work runs late.",
            CreatedAtUtc = new DateTimeOffset(2026, 8, 22, 7, 0, 0, TimeSpan.Zero),
        };

        var otherCoachStates = new[]
        {
            new CoachTrackedState(
                "Health Coach",
                [new GoalWithActionItems(healthGoal, [openItem, doneItem])],
                [healthReflection]),
            new CoachTrackedState("Kids Coach", [], []),
        };

        var result = CrossCoachContextFormatter.Format(otherCoachStates);

        Assert.Contains("Health Coach:", result);
        Assert.Contains("  - Sleep 8 hours", result);
        Assert.Contains("    [ ] No screens after 22:00 (due 2026-09-01)", result);
        Assert.Contains("    [x] Buy blackout curtains", result);
        Assert.Contains("  - (2026-08-22) I skip workouts when work runs late.", result);
        Assert.DoesNotContain(healthGoal.Id.ToString(), result);
        Assert.DoesNotContain(openItem.Id.ToString(), result);
        Assert.DoesNotContain("Kids Coach", result);
    }

    [Fact]
    public void Format_OmitsTheReflectionsHeading_WhenACoachHasGoalsButNoReflections()
    {
        var goal = Goal.Create("relationships", "Weekly date night");

        var otherCoachStates = new[]
        {
            new CoachTrackedState("Relationships Coach", [new GoalWithActionItems(goal, [])], []),
        };

        var result = CrossCoachContextFormatter.Format(otherCoachStates);

        Assert.Contains("  Goals:", result);
        Assert.DoesNotContain("Recent reflections:", result);
    }
}
