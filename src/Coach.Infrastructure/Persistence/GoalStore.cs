using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class GoalStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IGoalStore
{
    public async Task AddGoalAsync(Goal goal, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.Goals.Add(goal);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<Goal?> GetGoalAsync(Guid goalId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.Goals.AsNoTracking().FirstOrDefaultAsync(g => g.Id == goalId, cancellationToken);
    }

    public async Task<IReadOnlyList<GoalWithActionItems>> GetGoalsAsync(string coachSlug, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var goals = await dbContext.Goals
            .AsNoTracking()
            .Where(g => g.CoachSlug == coachSlug)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var goalIds = goals.Select(g => g.Id).ToList();
        var actionItems = await dbContext.ActionItems
            .AsNoTracking()
            .Where(a => goalIds.Contains(a.GoalId))
            .OrderBy(a => a.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var actionItemsByGoal = actionItems.ToLookup(a => a.GoalId);
        return goals
            .Select(g => new GoalWithActionItems(g, actionItemsByGoal[g.Id].ToList()))
            .ToList();
    }

    public async Task AddActionItemAsync(ActionItem actionItem, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ActionItems.Add(actionItem);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<ActionItem?> GetActionItemAsync(Guid actionItemId, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ActionItems.AsNoTracking().FirstOrDefaultAsync(a => a.Id == actionItemId, cancellationToken);
    }

    public async Task UpdateActionItemStatusAsync(ActionItem actionItem, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ActionItems.Attach(actionItem);
        dbContext.Entry(actionItem).Property(a => a.Status).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
