using Coach.Application.Services;

namespace Coach.Application.Tests;

public class CoachPersonaRegistryTests
{
    [Fact]
    public void TryGet_ReturnsCareerPersona_ForCareerSlug()
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet("career", out var persona);

        Assert.True(found);
        Assert.Equal("career", persona.Slug);
        Assert.Equal("Career Coach", persona.Name);
        Assert.False(string.IsNullOrWhiteSpace(persona.SystemPrompt));
        Assert.False(string.IsNullOrWhiteSpace(persona.Tone));
    }

    [Fact]
    public void TryGet_ReturnsFalse_ForUnknownSlug()
    {
        var registry = new CoachPersonaRegistry();

        var found = registry.TryGet("health", out var persona);

        Assert.False(found);
        Assert.Null(persona);
    }

    [Fact]
    public void GetAll_IncludesTheCareerPersona()
    {
        var registry = new CoachPersonaRegistry();

        var personas = registry.GetAll();

        Assert.Contains(personas, p => p.Slug == "career");
    }
}
