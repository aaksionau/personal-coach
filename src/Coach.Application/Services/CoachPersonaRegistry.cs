using System.Diagnostics.CodeAnalysis;
using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>Registry of coach personas. Only the Career persona is registered in v1.</summary>
public sealed class CoachPersonaRegistry
{
    private static readonly Dictionary<string, CoachPersona> Personas =
        new(StringComparer.Ordinal)
        {
            ["career"] = new CoachPersona(
                Slug: "career",
                Name: "Career Coach",
                SystemPrompt:
                    "You are the user's career coach. You help them think through career decisions, " +
                    "skill growth, work relationships, and career-related goals. Be direct and pragmatic: " +
                    "ask clarifying questions when a request is vague, push back gently on plans that " +
                    "seem likely to backfire, and prefer concrete next steps over generic platitudes.",
                Tone: "direct, pragmatic, encouraging"),
        };

    public bool TryGet(string slug, [MaybeNullWhen(false)] out CoachPersona persona) =>
        Personas.TryGetValue(slug, out persona);

    /// <summary>All registered personas -- the single source of truth for seeding the Coach schema.</summary>
    public IReadOnlyCollection<CoachPersona> GetAll() => Personas.Values;
}
