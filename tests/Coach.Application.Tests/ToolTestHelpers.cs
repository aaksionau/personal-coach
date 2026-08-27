using System.Text.Json;
using Microsoft.Extensions.AI;

namespace Coach.Application.Tests;

/// <summary>Shared helpers for exercising model tools (<see cref="AIFunction"/>s) in tests.</summary>
internal static class ToolTestHelpers
{
    public static AIFunction GetTool(IEnumerable<AITool> tools, string name) =>
        (AIFunction)tools.Single(tool => tool.Name == name);

    /// <summary>A tool invocation returns its string result as a JSON <see cref="JsonElement"/>.</summary>
    public static string AsText(object? result) => ((JsonElement)result!).GetString()!;
}
