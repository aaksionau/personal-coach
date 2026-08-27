using System.Text;
using Coach.Application.Models;
using Coach.Domain.Entities;

namespace Coach.Application.Services;

/// <summary>
/// Formats the cross-coach snapshot as text for the model's system prompt: a terse, id-free summary
/// of what each of the user's other coaches is currently tracking, so advice from the coach being
/// called can account for the other domains. The counterpart to <see cref="GoalContextFormatter"/>
/// and <see cref="ReflectionContextFormatter"/> for the cross-coach slice of a coach's context.
/// </summary>
public static class CrossCoachContextFormatter
{
    public static string Format(IReadOnlyList<CrossCoachSnapshot> snapshots)
    {
        var populated = snapshots.Where(HasSomething).ToList();
        if (populated.Count == 0)
        {
            return "The user's other coaches: nothing tracked yet.";
        }

        var builder = new StringBuilder(
            "What the user's other coaches are working on (for cross-domain awareness -- you cannot act on these):\n");
        foreach (var snapshot in populated)
        {
            builder.Append('\n').Append(snapshot.CoachName).Append(":\n");
            AppendGoals(builder, snapshot.Goals);
            AppendReflections(builder, snapshot.Reflections);
        }

        return builder.ToString();
    }

    private static bool HasSomething(CrossCoachSnapshot snapshot) =>
        snapshot.Goals.Count > 0 || snapshot.Reflections.Count > 0;

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
            ReflectionContextFormatter.AppendReflectionLine(builder, reflection, "  ");
        }
    }
}
