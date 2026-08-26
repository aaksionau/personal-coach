using Coach.Application.Models;
using Coach.Application.Services;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Tests;

public class GoalContextFormatterTests
{
    [Fact]
    public void Format_ReturnsAPlaceholder_WhenThereAreNoGoals()
    {
        var result = GoalContextFormatter.Format([]);

        Assert.Equal("Current goals: none yet.", result);
    }

    [Fact]
    public void Format_IncludesGoalAndActionItemIds_AndCheckedState()
    {
        var goal = Goal.Create("career", "Land a staff role");
        var openItem = ActionItem.Create(goal.Id, "Update resume", new DateOnly(2026, 9, 1));
        var doneItem = ActionItem.Create(goal.Id, "Set up alerts", null);
        doneItem.Status = ActionItemStatus.Done;

        var result = GoalContextFormatter.Format([new GoalWithActionItems(goal, [openItem, doneItem])]);

        Assert.Contains(goal.Title, result);
        Assert.Contains(goal.Id.ToString(), result);
        Assert.Contains($"[ ] Update resume (due 2026-09-01) (action item id: {openItem.Id})", result);
        Assert.Contains($"[x] Set up alerts (action item id: {doneItem.Id})", result);
    }

    [Fact]
    public void Format_NotesWhenAGoalHasNoActionItemsYet()
    {
        var goal = Goal.Create("career", "Land a staff role");

        var result = GoalContextFormatter.Format([new GoalWithActionItems(goal, [])]);

        Assert.Contains("(no action items yet)", result);
    }
}
