using Coach.Application.Services;

namespace Coach.Application.Tests;

public class CoachPersonaRegistryTests
{
    // slug, name, tone, a phrase the system prompt must contain (so a prompt/tone swap between
    // two personas can't pass this suite). ShortName is derived from Name and covered separately.
    public static TheoryData<string, string, string, string> ExpectedPersonas => new()
    {
        { "career", "Career Coach", "direct, pragmatic, encouraging", "career coach" },
        { "health", "Health Coach", "calm, supportive, evidence-minded", "health coach" },
        { "relationships", "Relationships Coach", "warm, candid, even-handed", "relationships coach" },
        { "kids", "Kids Coach", "grounded, reassuring, non-judgmental", "parenting coach" },
    };

    [Theory]
    [MemberData(nameof(ExpectedPersonas))]
    public void TryGet_ResolvesEachPersona_ToItsExpectedConfiguration(
        string slug, string expectedName, string expectedTone, string expectedPromptPhrase)
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet(slug, out var persona);

        Assert.True(found);
        Assert.Equal(slug, persona.Slug);
        Assert.Equal(expectedName, persona.Name);
        Assert.Equal(expectedTone, persona.Tone);
        Assert.Contains(expectedPromptPhrase, persona.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShortName_DropsTheCoachSuffix()
    {
        var registry = new CoachPersonaRegistry();

        Assert.True(registry.TryGet("relationships", out var persona));
        Assert.Equal("Relationships", persona.ShortName);
    }

    [Theory]
    [InlineData("health", true)]
    [InlineData("career", false)]
    [InlineData("relationships", false)]
    [InlineData("kids", false)]
    public void IncludesGarminMetrics_IsSetOnlyForTheHealthCoach(string slug, bool expected)
    {
        var registry = new CoachPersonaRegistry();

        Assert.True(registry.TryGet(slug, out var persona));
        Assert.Equal(expected, persona.IncludesGarminMetrics);
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
