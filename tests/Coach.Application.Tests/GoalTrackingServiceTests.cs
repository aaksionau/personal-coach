using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Coach.Domain.Enums;

namespace Coach.Application.Tests;

public class GoalTrackingServiceTests
{
    [Fact]
    public async Task CreateGoalAsync_PersistsAGoal_ScopedToTheCoach()
    {
        var service = new GoalTrackingService(new FakeGoalStore());

        var goal = await service.CreateGoalAsync("career", "  Land a staff role  ", CancellationToken.None);

        Assert.Equal("career", goal.CoachSlug);
        Assert.Equal("Land a staff role", goal.Title);
    }

    [Fact]
    public async Task CreateGoalAsync_Throws_ForBlankTitle()
    {
        var service = new GoalTrackingService(new FakeGoalStore());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateGoalAsync("career", "   ", CancellationToken.None));
    }

    [Fact]
    public async Task AddActionItemAsync_AddsAnOpenActionItem_UnderTheGoal()
    {
        var service = new GoalTrackingService(new FakeGoalStore());
        var goal = await service.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);

        var actionItem = await service.AddActionItemAsync(
            "career", goal.Id, "Update resume", new DateOnly(2026, 9, 1), CancellationToken.None);

        Assert.Equal(goal.Id, actionItem.GoalId);
        Assert.Equal("Update resume", actionItem.Description);
        Assert.Equal(ActionItemStatus.Open, actionItem.Status);
        Assert.Equal(new DateOnly(2026, 9, 1), actionItem.DueDate);
    }

    [Fact]
    public async Task AddActionItemAsync_Throws_WhenGoalDoesNotExist()
    {
        var service = new GoalTrackingService(new FakeGoalStore());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddActionItemAsync("career", Guid.NewGuid(), "Update resume", null, CancellationToken.None));
    }

    [Fact]
    public async Task AddActionItemAsync_Throws_WhenGoalBelongsToADifferentCoach()
    {
        var service = new GoalTrackingService(new FakeGoalStore());
        var goal = await service.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddActionItemAsync("health", goal.Id, "Update resume", null, CancellationToken.None));
    }

    [Fact]
    public async Task SetActionItemStatusAsync_UpdatesStatus_AndPersistsIt()
    {
        var store = new FakeGoalStore();
        var service = new GoalTrackingService(store);
        var goal = await service.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await service.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);

        var updated = await service.SetActionItemStatusAsync("career", actionItem.Id, ActionItemStatus.Done, CancellationToken.None);

        Assert.Equal(ActionItemStatus.Done, updated.Status);
        Assert.Equal(1, store.UpdateActionItemStatusCallCount);
    }

    [Fact]
    public async Task SetActionItemStatusAsync_Throws_WhenActionItemBelongsToADifferentCoach()
    {
        var service = new GoalTrackingService(new FakeGoalStore());
        var goal = await service.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var actionItem = await service.AddActionItemAsync("career", goal.Id, "Update resume", null, CancellationToken.None);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SetActionItemStatusAsync("health", actionItem.Id, ActionItemStatus.Done, CancellationToken.None));
    }

    [Fact]
    public async Task GetGoalsAsync_ReturnsOnlyGoalsForTheRequestedCoach()
    {
        var service = new GoalTrackingService(new FakeGoalStore());
        await service.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        await service.CreateGoalAsync("health", "Run a 10k", CancellationToken.None);

        var goals = await service.GetGoalsAsync("career", CancellationToken.None);

        Assert.Single(goals);
        Assert.Equal("Land a staff role", goals[0].Goal.Title);
    }
}
