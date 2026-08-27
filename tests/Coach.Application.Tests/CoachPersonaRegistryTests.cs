using Coach.Application.Services;

namespace Coach.Application.Tests;

public class CoachPersonaRegistryTests
{
    public static TheoryData<string, string> ExpectedPersonas => new()
    {
        { "career", "Career Coach" },
        { "health", "Health Coach" },
        { "relationships", "Relationships Coach" },
        { "kids", "Kids Coach" },
    };

    [Theory]
    [MemberData(nameof(ExpectedPersonas))]
    public void TryGet_ResolvesEachPersona_ToItsExpectedConfiguration(string slug, string expectedName)
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet(slug, out var persona);

        Assert.True(found);
        Assert.Equal(slug, persona.Slug);
        Assert.Equal(expectedName, persona.Name);
        Assert.False(string.IsNullOrWhiteSpace(persona.SystemPrompt));
        Assert.False(string.IsNullOrWhiteSpace(persona.Tone));
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
