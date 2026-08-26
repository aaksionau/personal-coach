using Coach.Application.Models;

namespace Coach.Application.Services;

/// <summary>Registry of coach personas. Only the Career persona is registered in v1.</summary>
public sealed class CoachPersonaRegistry
{
    private static readonly IReadOnlyDictionary<string, CoachPersona> Personas =
        new Dictionary<string, CoachPersona>(StringComparer.Ordinal)
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

    public CoachPersona Get(string slug)
    {
        if (Personas.TryGetValue(slug, out var persona))
        {
            return persona;
        }

        throw new KeyNotFoundException($"No coach persona registered for slug '{slug}'.");
    }
}
