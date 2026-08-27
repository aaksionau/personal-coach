using System.ComponentModel;
using Coach.Application.Services;
using Microsoft.Extensions.AI;

namespace Coach.Application.Tools;

/// <summary>
/// The one action the values wizard offers the model: distil the interview into a values summary
/// and save it. Mirrors <see cref="ReflectionTools"/> -- a plain typed method wrapped by
/// <see cref="AIFunctionFactory"/>. Not coach-scoped: the values profile is global.
/// <see cref="Saved"/> lets <see cref="Coach.Application.Agents.ValuesWizardService"/> tell whether the model completed the
/// wizard on a given turn.
/// </summary>
public sealed class ValuesProfileTools(ValuesProfileService valuesProfileService)
{
    public bool Saved { get; private set; }

    public IList<AITool> AsTools() =>
    [
        AIFunctionFactory.Create(
            SaveValuesProfileAsync,
            name: "save_values_profile",
            description:
                "Save the user's values profile. Call this once, at the end of the interview, with a "
                + "concise structured summary of what matters to the user across their career, health, "
                + "relationships, parenting, and life more broadly."),
    ];

    private Task<string> SaveValuesProfileAsync(
        [Description("The distilled values summary -- structured prose, a few short paragraphs or grouped bullet points.")] string summary,
        CancellationToken cancellationToken) =>
        ModelToolGuard.GuardedAsync(async () =>
        {
            await valuesProfileService.SaveProfileAsync(summary, cancellationToken);
            Saved = true;
            return "Saved the values profile. Let the user know it's stored and they can view or edit it any time on the Values page.";
        });
}
