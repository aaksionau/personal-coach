using Coach.Application.Services;

namespace Coach.Application.Tests;

public class CoachPersonaRegistryTests
{
    [Fact]
    public void Get_ReturnsCareerPersona_ForCareerSlug()
    {
        var registry = new CoachPersonaRegistry();

        var persona = registry.Get("career");

        Assert.Equal("career", persona.Slug);
        Assert.Equal("Career Coach", persona.Name);
        Assert.False(string.IsNullOrWhiteSpace(persona.SystemPrompt));
        Assert.False(string.IsNullOrWhiteSpace(persona.Tone));
    }

    [Fact]
    public void Get_Throws_ForUnknownSlug()
    {
        var registry = new CoachPersonaRegistry();

        Assert.Throws<KeyNotFoundException>(() => registry.Get("health"));
    }
}
