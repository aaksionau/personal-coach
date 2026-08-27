using System.ComponentModel;
using Coach.Application.Services;
using Coach.Domain.Enums;
using Microsoft.Extensions.AI;

namespace Coach.Application.Tools;

/// <summary>
/// Goal Tracking actions offered to the model as tools for one turn, scoped to a single coach
/// slug. Each tool is a plain typed method wrapped by <see cref="AIFunctionFactory"/>, which
/// reflects the method's parameters into the tool's JSON schema and binds a model tool call's
/// arguments back into them -- no hand-written schema or argument-parsing code to keep in sync.
/// </summary>
public sealed class GoalActionTools(GoalTrackingService goalTrackingService, string coachSlug)
{
    public IList<AITool> AsTools() =>
    [
        AIFunctionFactory.Create(
            CreateGoalAsync, name: "create_goal", description: "Create a new goal for the current coach."),
        AIFunctionFactory.Create(
            AddActionItemAsync, name: "add_action_item", description: "Add an action item under an existing goal."),
        AIFunctionFactory.Create(
            SetActionItemStatusAsync,
            name: "set_action_item_status",
            description: "Update an action item's status, e.g. when the user says they finished something."),
    ];

    private Task<string> CreateGoalAsync(
        [Description("Short goal title.")] string title,
        CancellationToken cancellationToken) =>
        ModelToolGuard.GuardedAsync(async () =>
        {
            var goal = await goalTrackingService.CreateGoalAsync(coachSlug, title, cancellationToken);
            return $"Created goal '{goal.Title}' (goal id: {goal.Id}).";
        });

    private Task<string> AddActionItemAsync(
        [Description("The goal id shown in the current goals list.")] Guid goalId,
        [Description("What needs to be done.")] string description,
        [Description("Optional due date, formatted YYYY-MM-DD.")] DateOnly? dueDate,
        CancellationToken cancellationToken) =>
        ModelToolGuard.GuardedAsync(async () =>
        {
            var actionItem = await goalTrackingService.AddActionItemAsync(coachSlug, goalId, description, dueDate, cancellationToken);
            return $"Added action item '{actionItem.Description}' (action item id: {actionItem.Id}).";
        });

    private Task<string> SetActionItemStatusAsync(
        [Description("The action item id shown in the current goals list.")] Guid actionItemId,
        [Description("Either 'open' or 'done'.")] string status,
        CancellationToken cancellationToken) =>
        ModelToolGuard.GuardedAsync(async () =>
        {
            var parsedStatus = string.Equals(status, "done", StringComparison.OrdinalIgnoreCase)
                ? ActionItemStatus.Done
                : ActionItemStatus.Open;
            var actionItem = await goalTrackingService.SetActionItemStatusAsync(coachSlug, actionItemId, parsedStatus, cancellationToken);
            return $"Action item '{actionItem.Description}' is now {actionItem.Status}.";
        });
}
