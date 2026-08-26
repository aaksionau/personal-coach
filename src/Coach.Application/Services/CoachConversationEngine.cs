using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Services;

/// <summary>
/// Drives one chat turn: builds context, persists the user's message, calls the model deployment
/// via <see cref="IChatCompletionClient"/>, and persists + returns the assistant's reply.
/// </summary>
public sealed class CoachConversationEngine(
    CoachContextBuilder contextBuilder,
    IChatMessageStore chatMessageStore,
    IChatCompletionClient chatCompletionClient)
{
    public async Task<string> SendMessageAsync(string coachSlug, string userMessage, CancellationToken cancellationToken)
    {
        var context = await contextBuilder.BuildAsync(coachSlug, cancellationToken);

        var userChatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            CoachSlug = coachSlug,
            Role = ChatMessageRole.User,
            Content = userMessage,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        await chatMessageStore.AddAsync(userChatMessage, cancellationToken);

        var conversation = new List<ChatMessage>(context.RecentMessages.Count + 1);
        conversation.AddRange(context.RecentMessages);
        conversation.Add(userChatMessage);

        var reply = await chatCompletionClient.GetReplyAsync(context.Persona.SystemPrompt, conversation, cancellationToken);

        var assistantChatMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            CoachSlug = coachSlug,
            Role = ChatMessageRole.Assistant,
            Content = reply,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
        await chatMessageStore.AddAsync(assistantChatMessage, cancellationToken);

        return reply;
    }
}
