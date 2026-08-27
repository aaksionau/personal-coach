using System.ComponentModel;
using Coach.Application.Services;
using Microsoft.Extensions.AI;

namespace Coach.Application.Tools;

/// <summary>
/// Reflection actions offered to the model as tools for one turn, scoped to a single coach slug.
/// Mirrors <see cref="GoalActionTools"/>: each tool is a plain typed method wrapped by
/// <see cref="AIFunctionFactory"/>, so the JSON schema and argument binding are reflected from the
/// method signature rather than hand-written.
/// </summary>
public sealed class ReflectionTools(ReflectionService reflectionService, string coachSlug)
{
    public IList<AITool> AsTools() =>
    [
        AIFunctionFactory.Create(
            RecordReflectionAsync,
            name: "record_reflection",
            description:
                "Record a reflection for the current coach: a short, standalone insight or recurring "
                + "theme that has emerged across recent check-ins. Use this when the conversation "
                + "surfaces a pattern worth remembering, not for routine goal or action-item updates."),
    ];

    private Task<string> RecordReflectionAsync(
        [Description("The reflection text -- a concise insight or theme drawn from recent conversations.")] string content,
        CancellationToken cancellationToken) =>
        ModelToolGuard.GuardedAsync(async () =>
        {
            var reflection = await reflectionService.RecordReflectionAsync(coachSlug, content, cancellationToken);
            return $"Recorded reflection (reflection id: {reflection.Id}).";
        });
}
