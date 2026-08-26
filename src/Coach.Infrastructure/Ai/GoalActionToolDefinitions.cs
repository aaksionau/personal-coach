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
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "title": { "type": "string", "description": "Short goal title." }
                    },
                    "required": ["title"]
                }
                """)),
        ChatTool.CreateFunctionTool(
            functionName: GoalActionToolName.AddActionItem,
            functionDescription: "Add an action item under an existing goal.",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "goal_id": { "type": "string", "description": "The goal id shown in the current goals list." },
                        "description": { "type": "string", "description": "What needs to be done." },
                        "due_date": { "type": "string", "description": "Optional due date, formatted YYYY-MM-DD." }
                    },
                    "required": ["goal_id", "description"]
                }
                """)),
        ChatTool.CreateFunctionTool(
            functionName: GoalActionToolName.SetActionItemStatus,
            functionDescription: "Update an action item's status, e.g. when the user says they finished something.",
            functionParameters: BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "action_item_id": { "type": "string", "description": "The action item id shown in the current goals list." },
                        "status": { "type": "string", "enum": ["open", "done"] }
                    },
                    "required": ["action_item_id", "status"]
                }
                """)),
    ];
}
