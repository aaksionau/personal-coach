using Coach.Application.Models;

namespace Coach.Application.Formatters;

/// <summary>
/// Assembles the system prompt for a coach turn from an already-built <see cref="CoachContext"/>:
/// the persona instructions followed by the values, goal, reflection, cross-coach, and calendar
/// slices, each rendered by its own formatter, plus -- for the Health coach only -- a Garmin
/// metrics slice. Split out from <see cref="Coach.Application.Agents.CoachConversationEngine"/>
/// so the prompt assembly -- including that another coach's state actually reaches the model -- is
/// testable without a model call.
/// </summary>
public static class CoachSystemPromptComposer
{
    public static string Compose(CoachContext context) =>
        context.Persona.SystemPrompt
        + "\n\n" + ValuesContextFormatter.Format(context.ValuesProfile)
        + "\n\n" + GoalContextFormatter.Format(context.OwnState.Goals)
        + "\n\n" + ReflectionContextFormatter.Format(context.OwnState.Reflections)
        + "\n\n" + CrossCoachContextFormatter.Format(context.OtherCoachStates)
        + "\n\n" + CalendarContextFormatter.Format(context.UpcomingEvents)
        + (context.Persona.IncludesGarminMetrics
            ? "\n\n" + GarminContextFormatter.Format(context.GarminMetrics)
            : string.Empty);
}
