using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Enums;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using DomainChatMessage = Coach.Domain.Entities.ChatMessage;

namespace Coach.Application.Services;

/// <summary>
/// Drives one chat turn: builds context, persists the user's message, calls the model deployment
/// via the Microsoft Agent Framework <see cref="AIAgent"/>, and persists + returns the assistant's
/// reply. Returns the persisted entities themselves so callers render the authoritative record of
/// the turn rather than reconstructing their own copy.
/// </summary>
public sealed class CoachConversationEngine(
    CoachContextBuilder contextBuilder,
    IChatMessageStore chatMessageStore,
    AIAgent agent,
    GoalTrackingService goalTrackingService,
    ReflectionService reflectionService)
{
    public async Task<ConversationTurn> SendMessageAsync(string coachSlug, string userMessage, CancellationToken cancellationToken)
    {
        var context = await contextBuilder.BuildAsync(coachSlug, cancellationToken);

        var userChatMessage = DomainChatMessage.Create(coachSlug, ChatMessageRole.User, userMessage);
        await chatMessageStore.AddAsync(userChatMessage, cancellationToken);

        var systemPrompt = context.Persona.SystemPrompt
            + "\n\n" + ValuesContextFormatter.Format(context.ValuesProfile)
            + "\n\n" + GoalContextFormatter.Format(context.Goals)
            + "\n\n" + ReflectionContextFormatter.Format(context.Reflections)
            + "\n\n" + CrossCoachContextFormatter.Format(context.CrossCoachSnapshots);

        var messages = new List<ChatMessage>(context.RecentMessages.Count + 2) { new(ChatRole.System, systemPrompt) };
        messages.AddRange(context.RecentMessages.Select(ToAiChatMessage));
        messages.Add(ToAiChatMessage(userChatMessage));

        List<AITool> tools =
        [
            .. new GoalActionTools(goalTrackingService, coachSlug).AsTools(),
            .. new ReflectionTools(reflectionService, coachSlug).AsTools(),
        ];
        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { Tools = tools });

        var response = await agent.RunAsync(messages, options: runOptions, cancellationToken: cancellationToken);

        var assistantChatMessage = DomainChatMessage.Create(coachSlug, ChatMessageRole.Assistant, response.Text);
        await chatMessageStore.AddAsync(assistantChatMessage, cancellationToken);

        return new ConversationTurn(userChatMessage, assistantChatMessage);
    }

    private static ChatMessage ToAiChatMessage(DomainChatMessage message) =>
        new(message.Role == ChatMessageRole.User ? ChatRole.User : ChatRole.Assistant, message.Content);
}
