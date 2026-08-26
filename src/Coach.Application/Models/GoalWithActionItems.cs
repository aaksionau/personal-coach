using Coach.Domain.Entities;

namespace Coach.Application.Models;

/// <summary>A goal composed with its action items -- built in the Application layer rather than via EF navigation.</summary>
public sealed record GoalWithActionItems(Goal Goal, IReadOnlyList<ActionItem> ActionItems);
