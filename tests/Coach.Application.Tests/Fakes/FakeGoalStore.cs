using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeGoalStore : IGoalStore
{
    private readonly Dictionary<Guid, Goal> _goals = [];
    private readonly Dictionary<Guid, ActionItem> _actionItems = [];

    public Task AddGoalAsync(Goal goal, CancellationToken cancellationToken)
    {
        _goals[goal.Id] = goal;
        return Task.CompletedTask;
    }

    public Task<Goal?> GetGoalAsync(Guid goalId, CancellationToken cancellationToken) =>
        Task.FromResult(_goals.GetValueOrDefault(goalId));

    public Task<IReadOnlyList<GoalWithActionItems>> GetGoalsAsync(string coachSlug, CancellationToken cancellationToken)
    {
        var result = _goals.Values
            .Where(g => g.CoachSlug == coachSlug)
            .OrderBy(g => g.CreatedAtUtc)
            .Select(g => new GoalWithActionItems(
                g,
                _actionItems.Values.Where(a => a.GoalId == g.Id).OrderBy(a => a.CreatedAtUtc).ToList()))
            .ToList();
        return Task.FromResult<IReadOnlyList<GoalWithActionItems>>(result);
    }

    public Task AddActionItemAsync(ActionItem actionItem, CancellationToken cancellationToken)
    {
        _actionItems[actionItem.Id] = actionItem;
        return Task.CompletedTask;
    }

    public Task<ActionItem?> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken) =>
        Task.FromResult(_actionItems.GetValueOrDefault(actionItemId));

    public int UpdateActionItemStatusCallCount { get; private set; }

    public Task UpdateActionItemStatusAsync(ActionItem actionItem, CancellationToken cancellationToken)
    {
        UpdateActionItemStatusCallCount++;
        _actionItems[actionItem.Id] = actionItem;
        return Task.CompletedTask;
    }
}
