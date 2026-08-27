using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Coach.Domain.Enums;
using Microsoft.Extensions.AI;
using static Coach.Application.Tests.ToolTestHelpers;

namespace Coach.Application.Tests;

public class GoalActionToolsTests
{
    [Fact]
    public async Task CreateGoal_PersistsTheGoal_ForTheCoach()
    {
        var store = new FakeGoalStore();
        var tool = GetTool(new GoalActionTools(new GoalTrackingService(store), "career").AsTools(), "create_goal");

        var result = await tool.InvokeAsync(new AIFunctionArguments { ["title"] = "Land a staff role" });

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        Assert.Single(goals);
        Assert.Equal("Land a staff role", goals[0].Goal.Title);
        Assert.Contains("Land a staff role", AsText(result));
    }

    [Fact]
    public async Task AddActionItem_PersistsUnderTheGoal_WithDueDate()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var tools = new GoalActionTools(goalService, "career");
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);

        var result = await GetTool(tools.AsTools(), "add_action_item").InvokeAsync(new AIFunctionArguments
        {
            ["goalId"] = goal.Id,
            ["description"] = "Update resume",
            ["dueDate"] = new DateOnly(2026, 9, 1),
        });

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        var actionItem = Assert.Single(goals[0].ActionItems);
        Assert.Equal("Update resume", actionItem.Description);
        Assert.Equal(new DateOnly(2026, 9, 1), actionItem.DueDate);
        Assert.Contains("Update resume", AsText(result));
    }

    [Fact]
    public async Task SetActionItemStatus_MarksTheActionItemDone()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var tools = new GoalActionTools(goalService, "career");
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await goalService.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);

        var result = await GetTool(tools.AsTools(), "set_action_item_status").InvokeAsync(new AIFunctionArguments
        {
            ["actionItemId"] = actionItem.Id,
            ["status"] = "done",
        });

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        Assert.Equal(ActionItemStatus.Done, goals[0].ActionItems[0].Status);
        Assert.Contains("Done", AsText(result));
    }

    [Fact]
    public async Task SetActionItemStatus_ReturnsAnErrorString_InsteadOfThrowing_ForACrossCoachActionItem()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await goalService.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);
        var tools = new GoalActionTools(goalService, "health");

        var result = await GetTool(tools.AsTools(), "set_action_item_status").InvokeAsync(new AIFunctionArguments
        {
            ["actionItemId"] = actionItem.Id,
            ["status"] = "done",
        });

        Assert.Contains("Could not complete the action", AsText(result));
    }
}
