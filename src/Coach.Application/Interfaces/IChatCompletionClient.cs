using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

/// <summary>Port to the underlying model deployment, isolating the Application layer from any specific SDK.</summary>
public interface IChatCompletionClient
{
    /// <summary>
    /// Sends one turn, offering Goal Tracking tools to the model. <paramref name="executeGoalAction"/>
    /// is invoked (tool name, arguments JSON) for each tool call the model requests; the adapter owns
    /// the SDK-specific loop of feeding results back until the model produces a final reply.
    /// </summary>
    Task<string> GetReplyAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversation,
        Func<string, string, CancellationToken, Task<string>> executeGoalAction,
        CancellationToken cancellationToken);
}
