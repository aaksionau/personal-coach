using Coach.Application.Formatters;
using Coach.Application.Models;
using Coach.Application.Services;
using Microsoft.Agents.AI;

namespace Coach.Application.Agents;

/// <summary>
/// Generates one ad-hoc due-date nudge text: resolves the coach persona for the action item's
/// domain, loads the values profile, has the model write a short message grounded in that one
/// action item, and returns it for the scheduler to send via <see cref="Interfaces.ISmsNotifier"/>.
/// Like <see cref="WeeklyDigestService"/> the model-calling glue here is untested by design; the
/// prompt assembly (<see cref="DueDateNudgePromptComposer"/>) is covered on its own.
/// </summary>
public sealed class DueDateNudgeService(
    CoachPersonaRegistry personaRegistry,
    ValuesProfileService valuesProfileService,
    AIAgent agent)
{
    /// <param name="today">The local calendar day the run is for -- used to phrase how close the due date is.</param>
    public async Task<string> GenerateAsync(PendingNudge pending, DateOnly today, CancellationToken cancellationToken)
    {
        var persona = personaRegistry.Get(pending.CoachSlug);
        var valuesProfile = await valuesProfileService.GetProfileAsync(cancellationToken);

        var systemPrompt = DueDateNudgePromptComposer.Compose(persona, pending, today, valuesProfile);

        return await agent.RunSingleTurnAsync(
            systemPrompt, "Write the nudge text for this action item.", cancellationToken);
    }
}
