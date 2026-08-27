using Coach.Application.Formatters;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class ReflectionContextFormatterTests
{
    [Fact]
    public void Format_ReturnsAPlaceholder_WhenThereAreNoReflections()
    {
        var result = ReflectionContextFormatter.Format([]);

        Assert.Equal("Recent reflections: none yet.", result);
    }

    [Fact]
    public void Format_ListsEachReflection_WithItsDate()
    {
        var reflection = new Reflection
        {
            Id = Guid.NewGuid(),
            CoachSlug = "career",
            Content = "I do my best thinking after a walk.",
            CreatedAtUtc = new DateTimeOffset(2026, 8, 20, 9, 30, 0, TimeSpan.Zero),
        };

        var result = ReflectionContextFormatter.Format([reflection]);

        Assert.Contains("- (2026-08-20) I do my best thinking after a walk.", result);
    }
}
