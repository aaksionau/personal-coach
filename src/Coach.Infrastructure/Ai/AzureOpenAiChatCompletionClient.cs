using Coach.Application.Interfaces;
using OpenAI.Chat;
using DomainChatMessage = Coach.Domain.Entities.ChatMessage;
using DomainChatMessageRole = Coach.Domain.Enums.ChatMessageRole;

namespace Coach.Infrastructure.Ai;

/// <summary>Adapts the Azure AI Foundry GPT-4o deployment (via the Azure OpenAI SDK) to <see cref="IChatCompletionClient"/>.</summary>
public sealed class AzureOpenAiChatCompletionClient(ChatClient chatClient) : IChatCompletionClient
{
    public async Task<string> GetReplyAsync(
        string systemPrompt,
        IReadOnlyList<DomainChatMessage> conversation,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var message in conversation)
        {
            messages.Add(message.Role == DomainChatMessageRole.User
                ? new UserChatMessage(message.Content)
                : new AssistantChatMessage(message.Content));
        }

        var completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
        return completion.Value.Content[0].Text;
    }
}
