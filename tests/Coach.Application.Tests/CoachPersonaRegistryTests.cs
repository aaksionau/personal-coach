using Coach.Application.Services;

namespace Coach.Application.Tests;

public class CoachPersonaRegistryTests
{
    // slug, name, short name, tone, a phrase the system prompt must contain (so a prompt/tone
    // swap between two personas can't pass this suite).
    public static TheoryData<string, string, string, string, string> ExpectedPersonas => new()
    {
        { "career", "Career Coach", "Career", "direct, pragmatic, encouraging", "career coach" },
        { "health", "Health Coach", "Health", "calm, supportive, evidence-minded", "health coach" },
        { "relationships", "Relationships Coach", "Relationships", "warm, candid, even-handed", "relationships coach" },
        { "kids", "Kids Coach", "Kids", "grounded, reassuring, non-judgmental", "parenting coach" },
    };

    [Theory]
    [MemberData(nameof(ExpectedPersonas))]
    public void TryGet_ResolvesEachPersona_ToItsExpectedConfiguration(
        string slug, string expectedName, string expectedShortName, string expectedTone, string expectedPromptPhrase)
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet(slug, out var persona);

        Assert.True(found);
        Assert.Equal(slug, persona.Slug);
        Assert.Equal(expectedName, persona.Name);
        Assert.Equal(expectedShortName, persona.ShortName);
        Assert.Equal(expectedTone, persona.Tone);
        Assert.Contains(expectedPromptPhrase, persona.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownSlug()
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet("finance", out var persona);

        Assert.False(found);
        Assert.Null(persona);
    }

    [Fact]
    public void GetAll_ReturnsExactlyTheFourRegisteredPersonas()
    {
        var registry = new CoachPersonaRegistry();

        var slugs = registry.GetAll().Select(p => p.Slug).OrderBy(s => s);

        Assert.Equal(new[] { "career", "health", "kids", "relationships" }, slugs);
    }
}
