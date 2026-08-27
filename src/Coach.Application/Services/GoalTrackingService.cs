using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Services;

/// <summary>
/// CRUD and status-transition logic for Goals/Action Items, scoped per coach. This is the public
/// interface Goal Tracking is tested against -- <see cref="IGoalStore"/> is swapped for a fake in
/// tests, the same way <see cref="Coach.Application.Builders.CoachContextBuilder"/> is tested against a fake message store.
/// </summary>
public sealed class GoalTrackingService(IGoalStore goalStore)
{
    public async Task<Goal> CreateGoalAsync(string coachSlug, string title, CancellationToken cancellationToken)
    {
        var trimmedTitle = title.Trim();
        if (trimmedTitle.Length == 0)
        {
            throw new ArgumentException("Goal title is required.", nameof(title));
        }

        var goal = Goal.Create(coachSlug, trimmedTitle);
        await goalStore.AddGoalAsync(goal, cancellationToken);
        return goal;
    }

    public async Task<ActionItem> AddActionItemAsync(
        string coachSlug, Guid goalId, string description, DateOnly? dueDate, CancellationToken cancellationToken)
    {
        var trimmedDescription = description.Trim();
        if (trimmedDescription.Length == 0)
        {
            throw new ArgumentException("Action item description is required.", nameof(description));
        }

        await GetGoalScopedToCoachAsync(goalId, coachSlug, cancellationToken);

        var actionItem = ActionItem.Create(goalId, trimmedDescription, dueDate);
        await goalStore.AddActionItemAsync(actionItem, cancellationToken);
        return actionItem;
    }

    public async Task<ActionItem> SetActionItemStatusAsync(
        string coachSlug, Guid actionItemId, ActionItemStatus status, CancellationToken cancellationToken)
    {
        var actionItem = await goalStore.GetActionItemAsync(actionItemId, cancellationToken)
            ?? throw new InvalidOperationException($"Action item '{actionItemId}' was not found.");

        await GetGoalScopedToCoachAsync(actionItem.GoalId, coachSlug, cancellationToken);

        actionItem.Status = status;
        await goalStore.UpdateActionItemStatusAsync(actionItem, cancellationToken);
        return actionItem;
    }

    public Task<IReadOnlyList<GoalWithActionItems>> GetGoalsAsync(string coachSlug, CancellationToken cancellationToken) =>
        goalStore.GetGoalsAsync(coachSlug, cancellationToken);

    private async Task<Goal> GetGoalScopedToCoachAsync(Guid goalId, string coachSlug, CancellationToken cancellationToken)
    {
        var goal = await goalStore.GetGoalAsync(goalId, cancellationToken)
            ?? throw new InvalidOperationException($"Goal '{goalId}' was not found.");

        if (!string.Equals(goal.CoachSlug, coachSlug, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Goal '{goalId}' does not belong to coach '{coachSlug}'.");
        }

        return goal;
    }
}
