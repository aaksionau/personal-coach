using Coach.Application.Services;
using Coach.Application.Tools;
using Coach.Application.Tests.Fakes;
using Microsoft.Extensions.AI;
using static Coach.Application.Tests.ToolTestHelpers;

namespace Coach.Application.Tests;

public class ValuesProfileToolsTests
{
    [Fact]
    public async Task SaveValuesProfile_PersistsTheSummary_AndFlagsSaved()
    {
        var store = new FakeValuesProfileStore();
        var tools = new ValuesProfileTools(new ValuesProfileService(store));
        var tool = GetTool(tools.AsTools(), "save_values_profile");

        var result = await tool.InvokeAsync(new AIFunctionArguments
        {
            ["summary"] = "Values craft mastery, protects family evenings, avoids roles that require relocation.",
        });

        Assert.True(tools.Saved);
        Assert.Equal(1, store.AddCount);
        Assert.Contains("Values page", AsText(result));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task SaveValuesProfile_ReturnsAnErrorString_InsteadOfThrowing_ForMissingSummary(string? summary)
    {
        var store = new FakeValuesProfileStore();
        var tools = new ValuesProfileTools(new ValuesProfileService(store));
        var tool = GetTool(tools.AsTools(), "save_values_profile");

        var result = await tool.InvokeAsync(new AIFunctionArguments { ["summary"] = summary });

        Assert.False(tools.Saved);
        Assert.Equal(0, store.AddCount);
        Assert.Contains("Could not complete the action", AsText(result));
    }
}
