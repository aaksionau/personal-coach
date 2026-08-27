using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

public interface IGoalStore
{
    Task AddGoalAsync(Goal goal, CancellationToken cancellationToken);

    Task<Goal?> GetGoalAsync(Guid goalId, CancellationToken cancellationToken);

    /// <summary>All goals for a coach, each with its action items, oldest first.</summary>
    Task<IReadOnlyList<GoalWithActionItems>> GetGoalsAsync(string coachSlug, CancellationToken cancellationToken);

    Task AddActionItemAsync(ActionItem actionItem, CancellationToken cancellationToken);

    Task<ActionItem?> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken);

    /// <summary>Persists <paramref name="actionItem"/>.Status only -- callers must have set it first.</summary>
    Task UpdateActionItemStatusAsync(ActionItem actionItem, CancellationToken cancellationToken);
}
