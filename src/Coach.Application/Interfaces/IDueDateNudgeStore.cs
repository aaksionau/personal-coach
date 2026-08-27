using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

/// <summary>
/// Backs the ad-hoc due-date nudge trigger of the Check-in Scheduler: finds the action items due a
/// nudge right now and records the ones that were sent. Separate from <see cref="IGoalStore"/>
/// because it also owns the nudge bookkeeping table, not just goal reads.
/// </summary>
public interface IDueDateNudgeStore
{
    /// <summary>
    /// Open action items across every coach whose due date is within <paramref name="leadTimeDays"/>
    /// of <paramref name="today"/> (or up to <paramref name="stopAfterOverdueDays"/> past it) and
    /// that have not already been nudged on <paramref name="today"/>. Each carries its goal, coach,
    /// and the count of days it has previously been nudged.
    /// </summary>
    Task<IReadOnlyList<PendingNudge>> GetPendingNudgesAsync(
        DateOnly today, int leadTimeDays, int stopAfterOverdueDays, CancellationToken cancellationToken);

    Task RecordNudgeSentAsync(DueDateNudge nudge, CancellationToken cancellationToken);
}
