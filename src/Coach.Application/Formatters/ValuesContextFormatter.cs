using Coach.Domain.Entities;

namespace Coach.Application.Formatters;

/// <summary>
/// Formats the user's values profile as text for the model's system prompt. The counterpart to
/// <see cref="GoalContextFormatter"/> and <see cref="ReflectionContextFormatter"/> for the values
/// slice of a coach's context -- injected identically into all four personas.
/// </summary>
public static class ValuesContextFormatter
{
    public static string Format(ValuesProfile? profile) =>
        profile is null
            ? "Values profile: not set up yet."
            : "The user's values profile -- keep advice aligned with what matters to them:\n" + profile.Content;
}
