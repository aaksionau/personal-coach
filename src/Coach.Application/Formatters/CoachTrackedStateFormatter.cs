using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Formatters;

/// <summary>
/// The shared render of "what a coach is currently tracking" -- an id-free block of goals,
/// action items, and recent reflections under the coach's name. Used by
/// <see cref="CrossCoachContextFormatter"/> (the cross-domain slice of a single coach turn) and
/// <see cref="WeeklyDigestPromptComposer"/> (all four coaches at once for the weekly digest); each
/// owns its own surrounding heading and empty-state sentence.
/// </summary>
internal static class CoachTrackedStateFormatter
{
    /// <summary>The states worth rendering: those with at least one goal or reflection.</summary>
    internal static List<CoachTrackedState> Populated(IReadOnlyList<CoachTrackedState> states) =>
        states.Where(s => s.Goals.Count > 0 || s.Reflections.Count > 0).ToList();

    /// <summary>
    /// Appends "\n{CoachName}:\n" followed by that coach's goals and recent reflections, indented.
    /// </summary>
    internal static void AppendState(StringBuilder builder, CoachTrackedState state)
    {
        builder.Append('\n').Append(state.CoachName).Append(":\n");
        AppendGoals(builder, state.Goals);
        AppendReflections(builder, state.Reflections);
    }

    private static void AppendGoals(StringBuilder builder, IReadOnlyList<GoalWithActionItems> goals)
    {
        if (goals.Count == 0)
        {
            return;
        }

        builder.Append("  Goals:\n");
        foreach (var (goal, actionItems) in goals)
        {
            builder.Append("  - ").Append(goal.Title).Append('\n');
            foreach (var actionItem in actionItems)
            {
                GoalContextFormatter.AppendActionItemLine(builder, actionItem, "    ");
                builder.Append('\n');
            }
        }
    }

    private static void AppendReflections(StringBuilder builder, IReadOnlyList<Reflection> reflections)
    {
        if (reflections.Count == 0)
        {
            return;
        }

        builder.Append("  Recent reflections:\n");
        foreach (var reflection in reflections)
        {
            builder.Append("  - (").Append(reflection.CreatedAtUtc.ToString("yyyy-MM-dd")).Append(") ")
                .Append(reflection.Content).Append('\n');
        }
    }
}
