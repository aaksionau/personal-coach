namespace Coach.Application.Models;

/// <summary>
/// Names shared between the Infrastructure tool declarations (what the model is offered) and
/// <see cref="Services.GoalActionExecutor"/> (what actually runs), so the two can't drift apart.
/// </summary>
public static class GoalActionToolName
{
    public const string CreateGoal = "create_goal";

    public const string AddActionItem = "add_action_item";

    public const string SetActionItemStatus = "set_action_item_status";
}

/// <summary>
/// Tool-call argument field names shared the same way as <see cref="GoalActionToolName"/>: the
/// Infrastructure tool JSON schemas and the Application records that deserialize a call's
/// arguments both reference these constants, so a rename on one side can't silently desync from
/// the other.
/// </summary>
public static class GoalActionFieldName
{
    public const string Title = "title";

    public const string GoalId = "goal_id";

    public const string Description = "description";

    public const string DueDate = "due_date";

    public const string ActionItemId = "action_item_id";

    public const string Status = "status";
}
