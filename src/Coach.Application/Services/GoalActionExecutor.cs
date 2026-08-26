using System.Text.Json;
using System.Text.Json.Serialization;
using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Enums;

namespace Coach.Application.Services;

/// <summary>
/// Parses and executes a model-requested Goal Tracking tool call. Kept separate from the
/// Conversation Engine / Infrastructure model glue specifically so it's testable without a model
/// call: given a tool name + arguments JSON, verify the resulting mutation through a fake
/// <see cref="IGoalStore"/>.
/// </summary>
public sealed class GoalActionExecutor(GoalTrackingService goalTrackingService) : IGoalActionExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<string> ExecuteAsync(string toolName, string argumentsJson, string coachSlug, CancellationToken cancellationToken)
    {
        try
        {
            return toolName switch
            {
                GoalActionToolName.CreateGoal => await CreateGoalAsync(argumentsJson, coachSlug, cancellationToken),
                GoalActionToolName.AddActionItem => await AddActionItemAsync(argumentsJson, coachSlug, cancellationToken),
                GoalActionToolName.SetActionItemStatus => await SetActionItemStatusAsync(argumentsJson, coachSlug, cancellationToken),
                _ => $"Unknown tool '{toolName}'.",
            };
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or JsonException)
        {
            return $"Could not complete the action: {ex.Message}";
        }
    }

    private async Task<string> CreateGoalAsync(string argumentsJson, string coachSlug, CancellationToken cancellationToken)
    {
        var args = Deserialize<CreateGoalArgs>(argumentsJson);
        var goal = await goalTrackingService.CreateGoalAsync(coachSlug, args.Title, cancellationToken);
        return $"Created goal '{goal.Title}' (goal id: {goal.Id}).";
    }

    private async Task<string> AddActionItemAsync(string argumentsJson, string coachSlug, CancellationToken cancellationToken)
    {
        var args = Deserialize<AddActionItemArgs>(argumentsJson);
        var actionItem = await goalTrackingService.AddActionItemAsync(
            coachSlug, args.GoalId, args.Description, args.DueDate, cancellationToken);
        return $"Added action item '{actionItem.Description}' (action item id: {actionItem.Id}).";
    }

    private async Task<string> SetActionItemStatusAsync(string argumentsJson, string coachSlug, CancellationToken cancellationToken)
    {
        var args = Deserialize<SetActionItemStatusArgs>(argumentsJson);
        var status = string.Equals(args.Status, "done", StringComparison.OrdinalIgnoreCase)
            ? ActionItemStatus.Done
            : ActionItemStatus.Open;
        var actionItem = await goalTrackingService.SetActionItemStatusAsync(
            coachSlug, args.ActionItemId, status, cancellationToken);
        return $"Action item '{actionItem.Description}' is now {actionItem.Status}.";
    }

    private static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, JsonOptions)
            ?? throw new ArgumentException($"Missing arguments for tool call (expected {typeof(T).Name}).");

    private sealed record CreateGoalArgs([property: JsonPropertyName(GoalActionFieldName.Title)] string Title);

    private sealed record AddActionItemArgs(
        [property: JsonPropertyName(GoalActionFieldName.GoalId)] Guid GoalId,
        [property: JsonPropertyName(GoalActionFieldName.Description)] string Description,
        [property: JsonPropertyName(GoalActionFieldName.DueDate)] DateOnly? DueDate);

    private sealed record SetActionItemStatusArgs(
        [property: JsonPropertyName(GoalActionFieldName.ActionItemId)] Guid ActionItemId,
        [property: JsonPropertyName(GoalActionFieldName.Status)] string Status);
}
