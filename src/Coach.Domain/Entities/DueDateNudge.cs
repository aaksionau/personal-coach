namespace Coach.Domain.Entities;

/// <summary>
/// One record per calendar day that a due-date nudge text was sent for an action item. Its only
/// jobs are once-per-day dedup (<see cref="NudgeDate"/>) so a scheduler restart or a second daily
/// run never double-texts, and letting the nudge prompt escalate its tone by counting how many
/// nudges an item has already had. Flat FK to <see cref="ActionItem"/>, no navigation.
/// </summary>
public sealed class DueDateNudge
{
    public required Guid Id { get; init; }

    public required Guid ActionItemId { get; init; }

    /// <summary>The local calendar day the nudge was sent -- the dedup key alongside the action item.</summary>
    public required DateOnly NudgeDate { get; init; }

    /// <summary>The action item's due date at the time of the nudge, kept for traceability.</summary>
    public required DateOnly DueDateAtNudge { get; init; }

    public required DateTimeOffset SentAtUtc { get; init; }

    public static DueDateNudge Create(Guid actionItemId, DateOnly nudgeDate, DateOnly dueDateAtNudge) => new()
    {
        Id = Guid.NewGuid(),
        ActionItemId = actionItemId,
        NudgeDate = nudgeDate,
        DueDateAtNudge = dueDateAtNudge,
        SentAtUtc = DateTimeOffset.UtcNow,
    };
}
