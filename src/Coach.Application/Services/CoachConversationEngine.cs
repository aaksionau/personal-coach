using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Services;

/// <summary>
/// Drives one chat turn: builds context, persists the user's message, calls the model deployment
/// via <see cref="IChatCompletionClient"/>, and persists + returns the assistant's reply. Returns
/// the persisted entities themselves so callers render the authoritative record of the turn
/// rather than reconstructing their own copy.
/// </summary>
public sealed class CoachConversationEngine(
    CoachContextBuilder contextBuilder,
    IChatMessageStore chatMessageStore,
    IChatCompletionClient chatCompletionClient,
    IGoalActionExecutor goalActionExecutor)
{
    public async Task<ConversationTurn> SendMessageAsync(string coachSlug, string userMessage, CancellationToken cancellationToken)
    {
        var context = await contextBuilder.BuildAsync(coachSlug, cancellationToken);

        var userChatMessage = ChatMessage.Create(coachSlug, ChatMessageRole.User, userMessage);
        await chatMessageStore.AddAsync(userChatMessage, cancellationToken);

        var conversation = new List<ChatMessage>(context.RecentMessages.Count + 1);
        conversation.AddRange(context.RecentMessages);
        conversation.Add(userChatMessage);

        var systemPrompt = context.Persona.SystemPrompt + "\n\n" + GoalContextFormatter.Format(context.Goals);

        var reply = await chatCompletionClient.GetReplyAsync(
            systemPrompt,
            conversation,
            (toolName, argumentsJson, ct) => goalActionExecutor.ExecuteAsync(toolName, argumentsJson, coachSlug, ct),
            cancellationToken);

        var assistantChatMessage = ChatMessage.Create(coachSlug, ChatMessageRole.Assistant, reply);
        await chatMessageStore.AddAsync(assistantChatMessage, cancellationToken);

        return new ConversationTurn(userChatMessage, assistantChatMessage);
    }
}
