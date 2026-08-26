using Coach.Application.Interfaces;
using OpenAI.Chat;
using DomainChatMessage = Coach.Domain.Entities.ChatMessage;
using DomainChatMessageRole = Coach.Domain.Enums.ChatMessageRole;

namespace Coach.Infrastructure.Ai;

/// <summary>Adapts the Azure AI Foundry GPT-4o deployment (via the Azure OpenAI SDK) to <see cref="IChatCompletionClient"/>.</summary>
public sealed class AzureOpenAiChatCompletionClient(ChatClient chatClient) : IChatCompletionClient
{
    /// <summary>Hard cap on tool-call round trips within one turn, so a misbehaving model can't loop forever.</summary>
    private const int MaxToolIterations = 4;

    public async Task<string> GetReplyAsync(
        string systemPrompt,
        IReadOnlyList<DomainChatMessage> conversation,
        Func<string, string, CancellationToken, Task<string>> executeGoalAction,
        CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };
        foreach (var message in conversation)
        {
            messages.Add(message.Role == DomainChatMessageRole.User
                ? new UserChatMessage(message.Content)
                : new AssistantChatMessage(message.Content));
        }

        var options = new ChatCompletionOptions();
        foreach (var tool in GoalActionToolDefinitions.All)
        {
            options.Tools.Add(tool);
        }

        for (var iteration = 0; iteration < MaxToolIterations; iteration++)
        {
            var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
            var response = completion.Value;

            if (response.FinishReason != ChatFinishReason.ToolCalls)
            {
                return response.Content.Count > 0 ? response.Content[0].Text : string.Empty;
            }

            messages.Add(new AssistantChatMessage(response));

            // The model can request several tool calls in one response; it hasn't seen any of
            // their results yet, so they're independent and safe to run concurrently.
            var toolCalls = response.ToolCalls.ToList();
            var results = await Task.WhenAll(toolCalls.Select(toolCall =>
                executeGoalAction(toolCall.FunctionName, toolCall.FunctionArguments.ToString(), cancellationToken)));

            for (var i = 0; i < toolCalls.Count; i++)
            {
                messages.Add(new ToolChatMessage(toolCalls[i].Id, results[i]));
            }
        }

        return "I made some updates but I'm having trouble finishing my reply -- check the Goals page for the latest state.";
    }
}
