using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Coach.Application.Agents;

/// <summary>
/// Shared glue for the agents that make one stateless model call from a composed system prompt plus
/// a fixed user instruction (<see cref="WeeklyDigestService"/>, <see cref="DueDateNudgeService"/>) --
/// as opposed to the multi-turn, tool-carrying calls in <see cref="CoachConversationEngine"/> and
/// <see cref="ValuesWizardService"/>.
/// </summary>
internal static class SingleTurnAgent
{
    public static async Task<string> RunSingleTurnAsync(
        this AIAgent agent, string systemPrompt, string userInstruction, CancellationToken cancellationToken)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userInstruction),
        };

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);
        return response.Text;
    }
}
