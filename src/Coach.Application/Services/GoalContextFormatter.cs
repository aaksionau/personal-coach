using System.Text;
using Coach.Application.Models;
using Coach.Domain.Enums;

namespace Coach.Application.Services;

/// <summary>
/// Formats current goal/action-item state as text for the model's system prompt, including ids so
/// the model can reference an exact goal/action item when it requests a Goal Tracking tool call.
/// </summary>
public static class GoalContextFormatter
{
    public static string Format(IReadOnlyList<GoalWithActionItems> goals)
    {
        if (goals.Count == 0)
        {
            return "Current goals: none yet.";
        }

        var builder = new StringBuilder("Current goals and action items (use the ids below when calling a Goal Tracking tool):\n");
        foreach (var (goal, actionItems) in goals)
        {
            builder.Append("- ").Append(goal.Title).Append(" (goal id: ").Append(goal.Id).Append(")\n");

            if (actionItems.Count == 0)
            {
                builder.Append("  (no action items yet)\n");
                continue;
            }

            foreach (var actionItem in actionItems)
            {
                var box = actionItem.Status == ActionItemStatus.Done ? "[x]" : "[ ]";
                var due = actionItem.DueDate is { } dueDate ? $" (due {dueDate:yyyy-MM-dd})" : string.Empty;
                builder.Append("  ").Append(box).Append(' ').Append(actionItem.Description).Append(due)
                    .Append(" (action item id: ").Append(actionItem.Id).Append(")\n");
            }
        }

        return builder.ToString();
    }
}
