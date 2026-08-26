using Coach.Application.Models;
using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Coach.Domain.Enums;

namespace Coach.Application.Tests;

public class GoalActionExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_CreateGoal_PersistsTheGoal_ForTheCoach()
    {
        var store = new FakeGoalStore();
        var executor = new GoalActionExecutor(new GoalTrackingService(store));

        var result = await executor.ExecuteAsync(
            GoalActionToolName.CreateGoal, """{"title":"Land a staff role"}""", "career", CancellationToken.None);

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        Assert.Single(goals);
        Assert.Equal("Land a staff role", goals[0].Goal.Title);
        Assert.Contains("Land a staff role", result);
    }

    [Fact]
    public async Task ExecuteAsync_AddActionItem_PersistsUnderTheGoal_WithDueDate()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var executor = new GoalActionExecutor(goalService);
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);

        var result = await executor.ExecuteAsync(
            GoalActionToolName.AddActionItem,
            $$"""{"goal_id":"{{goal.Id}}","description":"Update resume","due_date":"2026-09-01"}""",
            "career",
            CancellationToken.None);

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        var actionItem = Assert.Single(goals[0].ActionItems);
        Assert.Equal("Update resume", actionItem.Description);
        Assert.Equal(new DateOnly(2026, 9, 1), actionItem.DueDate);
        Assert.Contains("Update resume", result);
    }

    [Fact]
    public async Task ExecuteAsync_SetActionItemStatus_MarksTheActionItemDone()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var executor = new GoalActionExecutor(goalService);
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await goalService.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);

        var result = await executor.ExecuteAsync(
            GoalActionToolName.SetActionItemStatus,
            $$"""{"action_item_id":"{{actionItem.Id}}","status":"done"}""",
            "career",
            CancellationToken.None);

        var goals = await store.GetGoalsAsync("career", CancellationToken.None);
        Assert.Equal(ActionItemStatus.Done, goals[0].ActionItems[0].Status);
        Assert.Contains("Done", result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAnErrorString_InsteadOfThrowing_ForACrossCoachActionItem()
    {
        var store = new FakeGoalStore();
        var goalService = new GoalTrackingService(store);
        var executor = new GoalActionExecutor(goalService);
        var goal = await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await goalService.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);

        var result = await executor.ExecuteAsync(
            GoalActionToolName.SetActionItemStatus,
            $$"""{"action_item_id":"{{actionItem.Id}}","status":"done"}""",
            "health",
            CancellationToken.None);

        Assert.Contains("Could not complete the action", result);
    }

    [Fact]
    public async Task ExecuteAsync_ReturnsAnErrorString_ForAnUnknownTool()
    {
        var executor = new GoalActionExecutor(new GoalTrackingService(new FakeGoalStore()));

        var result = await executor.ExecuteAsync("delete_everything", "{}", "career", CancellationToken.None);

        Assert.Contains("Unknown tool", result);
    }
}
