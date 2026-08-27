using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Formatters;

/// <summary>
/// Builds the system prompt for the weekly check-in digest: instructions for writing one short SMS
/// that spans all four coaches, followed by each coach's current goal/action-item/reflection state,
/// the user's upcoming calendar events, and their values profile. Pure and separate from
/// <see cref="Coach.Application.Agents.WeeklyDigestService"/> so the assembly -- in particular that
/// every coach's state reaches the model -- is testable without a model call.
/// </summary>
public static class WeeklyDigestPromptComposer
{
    /// <summary>
    /// A soft ceiling passed to the model, not enforced here -- long multipart texts still deliver
    /// through the Fi gateway, but the digest is meant to be a glanceable nudge, not a report.
    /// </summary>
    public const int TargetCharacterBudget = 400;

    private static readonly string Instructions =
        "You are writing the user's single weekly check-in text message. It covers all four areas of "
        + "their coaching at once -- career, health, relationships, and parenting -- in one message, "
        + "not four. Write it in a warm, direct coaching voice, as plain text with no markdown, "
        + "headings, or links. Lead with what is due or slipping, then anything notable worth a "
        + $"check-in; skip areas with nothing to say. Keep it under about {TargetCharacterBudget} "
        + "characters. Do not ask the user to reply by text -- they always reply by opening the app.";

    public static string Compose(
        IReadOnlyList<CoachTrackedState> coachStates,
        IReadOnlyList<CalendarEvent> upcomingEvents,
        ValuesProfile? valuesProfile)
    {
        var builder = new StringBuilder(Instructions).Append("\n\n");

        builder.Append(ValuesContextFormatter.Format(valuesProfile)).Append("\n\n");

        var populated = CoachTrackedStateFormatter.Populated(coachStates);
        if (populated.Count == 0)
        {
            builder.Append("Across all four coaches: nothing is being tracked yet.");
        }
        else
        {
            builder.Append("What each coach is currently tracking:\n");
            foreach (var state in populated)
            {
                CoachTrackedStateFormatter.AppendState(builder, state);
            }
        }

        builder.Append('\n').Append(CalendarContextFormatter.Format(upcomingEvents));

        return builder.ToString();
    }
}
