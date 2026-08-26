using Coach.Application.Models;
using OpenAI.Chat;

namespace Coach.Infrastructure.Ai;

/// <summary>OpenAI SDK tool declarations for Goal Tracking, offered to the model on every turn.</summary>
internal static class GoalActionToolDefinitions
{
    public static IReadOnlyList<ChatTool> All { get; } =
    [
        ChatTool.CreateFunctionTool(
            functionName: GoalActionToolName.CreateGoal,
            functionDescription: "Create a new goal for the current coach.",
            functionParameters: BinaryData.FromString($$"""
                {
                    "type": "object",
                    "properties": {
                        "{{GoalActionFieldName.Title}}": { "type": "string", "description": "Short goal title." }
                    },
                    "required": ["{{GoalActionFieldName.Title}}"]
                }
                """)),
        ChatTool.CreateFunctionTool(
            functionName: GoalActionToolName.AddActionItem,
            functionDescription: "Add an action item under an existing goal.",
            functionParameters: BinaryData.FromString($$"""
                {
                    "type": "object",
                    "properties": {
                        "{{GoalActionFieldName.GoalId}}": { "type": "string", "description": "The goal id shown in the current goals list." },
                        "{{GoalActionFieldName.Description}}": { "type": "string", "description": "What needs to be done." },
                        "{{GoalActionFieldName.DueDate}}": { "type": "string", "description": "Optional due date, formatted YYYY-MM-DD." }
                    },
                    "required": ["{{GoalActionFieldName.GoalId}}", "{{GoalActionFieldName.Description}}"]
                }
                """)),
        ChatTool.CreateFunctionTool(
            functionName: GoalActionToolName.SetActionItemStatus,
            functionDescription: "Update an action item's status, e.g. when the user says they finished something.",
            functionParameters: BinaryData.FromString($$"""
                {
                    "type": "object",
                    "properties": {
                        "{{GoalActionFieldName.ActionItemId}}": { "type": "string", "description": "The action item id shown in the current goals list." },
                        "{{GoalActionFieldName.Status}}": { "type": "string", "enum": ["open", "done"] }
                    },
                    "required": ["{{GoalActionFieldName.ActionItemId}}", "{{GoalActionFieldName.Status}}"]
                }
                """)),
    ];
}
