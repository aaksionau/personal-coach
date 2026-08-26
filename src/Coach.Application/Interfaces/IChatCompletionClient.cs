using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

/// <summary>Port to the underlying model deployment, isolating the Application layer from any specific SDK.</summary>
public interface IChatCompletionClient
{
    Task<string> GetReplyAsync(
        string systemPrompt,
        IReadOnlyList<ChatMessage> conversation,
        CancellationToken cancellationToken);
}
