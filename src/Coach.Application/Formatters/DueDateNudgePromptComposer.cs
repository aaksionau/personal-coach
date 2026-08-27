using System.Globalization;
using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Formatters;

/// <summary>
/// Builds the system prompt for a single ad-hoc due-date nudge: the coach persona, the one goal and
/// action item the nudge is about (with how close the due date is and how many times the item has
/// already been nudged), the values profile, then instructions for writing one short SMS. Pure and
/// separate from <see cref="Coach.Application.Agents.DueDateNudgeService"/> so the assembly -- in
/// particular that the specific action item reaches the model and the message stays scoped to it --
/// is testable without a model call. The digest counterpart is <see cref="WeeklyDigestPromptComposer"/>.
/// </summary>
public static class DueDateNudgePromptComposer
{
    /// <summary>
    /// A soft ceiling passed to the model, not enforced here -- a nudge is a single glanceable line,
    /// tighter than the weekly digest's budget.
    /// </summary>
    private const int TargetCharacterBudget = 300;

    private static readonly string Instructions =
        "You are writing the user a single short text message nudging them about ONE specific action "
        + "item whose due date is approaching. Write it in your coaching voice as plain text with no "
        + "markdown, headings, or links. Name the action item and what it is in service of, and give "
        + "one concrete push toward it. Stay on this one item -- this is a focused nudge, not the "
        + "weekly cross-coach check-in, so do not summarise their other goals or coaches. Match your "
        + "urgency to how close (or overdue) the item is and to how many times it has already been "
        + $"nudged. Keep it under about {TargetCharacterBudget} characters. Do not ask the user to "
        + "reply by text -- they always reply by opening the app.";

    public static string Compose(
        CoachPersona persona,
        PendingNudge pending,
        DateOnly today,
        ValuesProfile? valuesProfile)
    {
        var builder = new StringBuilder(persona.SystemPrompt).Append("\n\n");
        builder.Append("Your coaching tone: ").Append(persona.Tone).Append("\n\n");
        builder.Append(Instructions).Append("\n\n");
        builder.Append(ValuesContextFormatter.Format(valuesProfile)).Append("\n\n");

        builder.Append("The action item to nudge about:\n");
        builder.Append("- Goal: ").Append(pending.GoalTitle).Append('\n');
        builder.Append("- Action item: ").Append(pending.ActionItemDescription).Append('\n');
        builder.Append("- ").Append(DueDescription(pending.DueDate, today)).Append('\n');
        builder.Append("- ").Append(NudgeHistoryDescription(pending.PriorNudgeCount)).Append('\n');

        return builder.ToString();
    }

    private static string DueDescription(DateOnly due, DateOnly today)
    {
        var days = due.DayNumber - today.DayNumber;
        var iso = due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return days switch
        {
            > 1 => $"Due {iso}, {days} days from now.",
            1 => $"Due {iso}, tomorrow.",
            0 => $"Due {iso}, today.",
            -1 => $"Was due {iso}, 1 day overdue.",
            _ => $"Was due {iso}, {-days} days overdue.",
        };
    }

    private static string NudgeHistoryDescription(int priorNudgeCount) => priorNudgeCount switch
    {
        <= 0 => "This is the first nudge about this item.",
        1 => "This item has been nudged once before.",
        _ => $"This item has been nudged {priorNudgeCount} times before.",
    };
}
