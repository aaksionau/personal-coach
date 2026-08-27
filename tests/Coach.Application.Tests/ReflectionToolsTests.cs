using System.Text.Json;
using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Microsoft.Extensions.AI;

namespace Coach.Application.Tests;

public class ReflectionToolsTests
{
    [Fact]
    public async Task RecordReflection_PersistsTheReflection_ForTheCoach()
    {
        var store = new FakeReflectionStore();
        var tool = GetTool(new ReflectionTools(new ReflectionService(store), "career"), "record_reflection");

        var result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["content"] = "I keep overcommitting when asked in the moment.",
        });

        var reflection = Assert.Single(store.Added);
        Assert.Equal("career", reflection.CoachSlug);
        Assert.Equal("I keep overcommitting when asked in the moment.", reflection.Content);
        Assert.Contains(reflection.Id.ToString(), AsText(result));
    }

    [Fact]
    public async Task RecordReflection_ReturnsAnErrorString_InsteadOfThrowing_ForBlankContent()
    {
        var store = new FakeReflectionStore();
        var tool = GetTool(new ReflectionTools(new ReflectionService(store), "career"), "record_reflection");

        var result = await tool.InvokeAsync(new AIFunctionArguments { ["content"] = "   " });

        Assert.Empty(store.Added);
        Assert.Contains("Could not complete the action", AsText(result));
    }

    private static AIFunction GetTool(ReflectionTools tools, string name) =>
        (AIFunction)tools.AsTools().Single(tool => tool.Name == name);

    private static string AsText(object? result) => ((JsonElement)result!).GetString()!;
}
