using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>
/// An open action item whose due date is close enough (or already past) to warrant a nudge text,
/// paired with the goal it sits under and the coach whose voice the nudge is written in.
/// <see cref="PriorNudgeCount"/> is how many days this item has already been nudged -- fed to the
/// prompt so a repeat nudge reads more urgently than the first. Built in the Application layer
/// rather than via EF navigation, like <see cref="GoalWithActionItems"/>.
/// </summary>
public sealed record PendingNudge(
    string CoachSlug,
    string GoalTitle,
    ActionItem ActionItem,
    int PriorNudgeCount);
