namespace Coach.Application.Interfaces;

/// <summary>
/// Executes a single model-requested tool call against Goal Tracking and reports back a short
/// result string for the model to see -- errors are returned as text, not thrown, since a bad
/// tool call is untrusted model output, not a bug at this boundary.
/// </summary>
public interface IGoalActionExecutor
{
    Task<string> ExecuteAsync(string toolName, string argumentsJson, string coachSlug, CancellationToken cancellationToken);
}
