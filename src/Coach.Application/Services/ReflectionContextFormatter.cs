using System.Text;
using Coach.Domain.Entities;

namespace Coach.Application.Services;

/// <summary>
/// Formats recent reflections as text for the model's system prompt -- oldest first, each dated so
/// the model can situate a reflection in time. The counterpart to <see cref="GoalContextFormatter"/>
/// for the reflection slice of a coach's structured state.
/// </summary>
public static class ReflectionContextFormatter
{
    public static string Format(IReadOnlyList<Reflection> reflections)
    {
        if (reflections.Count == 0)
        {
            return "Recent reflections: none yet.";
        }

        var builder = new StringBuilder("Recent reflections (most recent last):\n");
        foreach (var reflection in reflections)
        {
            AppendReflectionLine(builder, reflection, string.Empty);
        }

        return builder.ToString();
    }

    /// <summary>
    /// Writes one dated reflection line at <paramref name="indent"/>. Shared with
    /// <see cref="CrossCoachContextFormatter"/> so the date format lives in one place.
    /// </summary>
    internal static void AppendReflectionLine(StringBuilder builder, Reflection reflection, string indent) =>
        builder.Append(indent).Append("- (").Append(reflection.CreatedAtUtc.ToString("yyyy-MM-dd")).Append(") ")
            .Append(reflection.Content).Append('\n');
}
