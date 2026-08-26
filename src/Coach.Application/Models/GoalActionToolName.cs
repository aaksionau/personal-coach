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
