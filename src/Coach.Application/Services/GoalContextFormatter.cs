using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;
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
                AppendActionItemLine(builder, actionItem, "  ");
                builder.Append(" (action item id: ").Append(actionItem.Id).Append(")\n");
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes the id-free part of an action-item line -- checkbox, description, and due date -- at
    /// <paramref name="indent"/>, with no trailing newline. Shared with
    /// <see cref="CrossCoachContextFormatter"/>, which renders action items without ids.
    /// </summary>
    internal static void AppendActionItemLine(StringBuilder builder, ActionItem actionItem, string indent)
    {
        var box = actionItem.Status == ActionItemStatus.Done ? "[x]" : "[ ]";
        var due = actionItem.DueDate is { } dueDate ? $" (due {dueDate:yyyy-MM-dd})" : string.Empty;
        builder.Append(indent).Append(box).Append(' ').Append(actionItem.Description).Append(due);
    }
}
