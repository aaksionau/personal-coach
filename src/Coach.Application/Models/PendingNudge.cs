namespace Coach.Application.Models;

/// <summary>
/// An open action item whose due date is close enough (or already past) to warrant a nudge text,
/// flattened at the store seam to just what the nudge needs: the coach whose voice it is written in,
/// the goal it sits under, the action item's id and description, its (always-present) due date, and
/// <see cref="PriorNudgeCount"/> -- how many days it has already been nudged, fed to the prompt so a
/// repeat nudge reads more urgently than the first. Projecting here rather than carrying the raw
/// entity (like <see cref="GoalWithActionItems"/> does) keeps the "always dated" guarantee in one
/// place instead of re-checking it at every use.
/// </summary>
public sealed record PendingNudge(
    string CoachSlug,
    string GoalTitle,
    Guid ActionItemId,
    string ActionItemDescription,
    DateOnly DueDate,
    int PriorNudgeCount);
