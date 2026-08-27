using System.Text;
using Coach.Application.Models;

namespace Coach.Application.Formatters;

/// <summary>
/// Formats the other coaches' tracked state as text for the model's system prompt: a terse, id-free
/// summary of what each of the user's other coaches is currently tracking, so advice from the coach
/// being called can account for the other domains. The counterpart to <see cref="GoalContextFormatter"/>
/// and <see cref="ReflectionContextFormatter"/> for the cross-coach slice of a coach's context.
/// </summary>
public static class CrossCoachContextFormatter
{
    public static string Format(IReadOnlyList<CoachTrackedState> otherCoachStates)
    {
        var populated = CoachTrackedStateFormatter.Populated(otherCoachStates);
        if (populated.Count == 0)
        {
            return "The user's other coaches: nothing tracked yet.";
        }

        var builder = new StringBuilder(
            "What the user's other coaches are working on (for cross-domain awareness -- you cannot act on these):\n");
        foreach (var state in populated)
        {
            CoachTrackedStateFormatter.AppendState(builder, state);
        }

        return builder.ToString();
    }
}
