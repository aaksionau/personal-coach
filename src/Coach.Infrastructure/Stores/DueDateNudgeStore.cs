using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Coach.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Stores;

public sealed class DueDateNudgeStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IDueDateNudgeStore
{
    public async Task<IReadOnlyList<PendingNudge>> GetPendingNudgesAsync(
        DateOnly today, int leadTimeDays, int stopAfterOverdueDays, CancellationToken cancellationToken)
    {
        var dueBy = today.AddDays(leadTimeDays);
        var notBefore = today.AddDays(-stopAfterOverdueDays);

        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        var rows = await (
            from actionItem in dbContext.ActionItems.AsNoTracking()
            join goal in dbContext.Goals.AsNoTracking() on actionItem.GoalId equals goal.Id
            where actionItem.Status == ActionItemStatus.Open
                && actionItem.DueDate != null
                && actionItem.DueDate >= notBefore
                && actionItem.DueDate <= dueBy
                && !dbContext.DueDateNudges.Any(n => n.ActionItemId == actionItem.Id && n.NudgeDate == today)
            select new
            {
                goal.CoachSlug,
                goal.Title,
                actionItem.Id,
                actionItem.Description,
                actionItem.DueDate,
                PriorNudgeCount = dbContext.DueDateNudges.Count(n => n.ActionItemId == actionItem.Id),
            }).ToListAsync(cancellationToken);

        return rows
            .Select(r => new PendingNudge(r.CoachSlug, r.Title, r.Id, r.Description, r.DueDate!.Value, r.PriorNudgeCount))
            .ToList();
    }

    public async Task RecordNudgeSentAsync(DueDateNudge nudge, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.DueDateNudges.Add(nudge);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
