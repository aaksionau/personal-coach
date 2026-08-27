using Coach.Application.Services;
using Coach.Application.Tools;
using Coach.Application.Tests.Fakes;
using Microsoft.Extensions.AI;
using static Coach.Application.Tests.ToolTestHelpers;

namespace Coach.Application.Tests;

public class ReflectionToolsTests
{
    [Fact]
    public async Task RecordReflection_PersistsTheReflection_ForTheCoach()
    {
        var store = new FakeReflectionStore();
        var tool = GetTool(new ReflectionTools(new ReflectionService(store), "career").AsTools(), "record_reflection");

        var result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["content"] = "I keep overcommitting when asked in the moment.",
        });

        var reflection = Assert.Single(store.Added);
        Assert.Equal("career", reflection.CoachSlug);
        Assert.Equal("I keep overcommitting when asked in the moment.", reflection.Content);
        Assert.Contains(reflection.Id.ToString(), AsText(result));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RecordReflection_ReturnsAnErrorString_InsteadOfThrowing_ForMissingContent(string? content)
    {
        var store = new FakeReflectionStore();
        var tool = GetTool(new ReflectionTools(new ReflectionService(store), "career").AsTools(), "record_reflection");

        var result = await tool.InvokeAsync(new AIFunctionArguments { ["content"] = content });

        Assert.Empty(store.Added);
        Assert.Contains("Could not complete the action", AsText(result));
    }
}
