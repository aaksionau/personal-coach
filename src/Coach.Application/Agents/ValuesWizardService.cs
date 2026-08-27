using Coach.Application.Models;
using Coach.Application.Services;
using Coach.Application.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Coach.Application.Agents;

/// <summary>
/// Drives the one-time guided values wizard: the model interviews the user, then distils and saves
/// a values profile via <see cref="ValuesProfileTools"/>. The conversation is not persisted -- the
/// page passes the running transcript back in on each turn -- so this is a thin per-turn call, like
/// <see cref="CoachConversationEngine"/>. The model-calling glue here is intentionally untested
/// (see the testing conventions); <see cref="ValuesProfileService"/> and
/// <see cref="ValuesProfileTools"/> carry the tested behaviour.
/// </summary>
public sealed class ValuesWizardService(AIAgent agent, ValuesProfileService valuesProfileService)
{
    private const string SystemPrompt =
        "You are guiding the user through a one-time conversation to distil their personal values. "
        + "The result is a foundational input to four separate coaches (career, health, relationships, "
        + "parenting), not a coaching relationship of its own. Interview the user across those four "
        + "areas and their life priorities more broadly: what they care about, what trade-offs they "
        + "are and aren't willing to make, what a good life looks like to them. Ask one focused "
        + "question at a time, build on their answers, and keep it to roughly five to eight exchanges. "
        + "When you have enough, call the save_values_profile tool with a concise, structured summary "
        + "(a few short paragraphs or grouped bullet points, written in the third person about the "
        + "user), then tell the user it's saved and that they can view or edit it any time on the "
        + "Values page. Do not call the tool more than once.";

    public async Task<ValuesWizardReply> ContinueAsync(
        IReadOnlyList<ValuesWizardMessage> history, string userMessage, CancellationToken cancellationToken)
    {
        var tools = new ValuesProfileTools(valuesProfileService);

        var messages = new List<ChatMessage>(history.Count + 2) { new(ChatRole.System, SystemPrompt) };
        messages.AddRange(history.Select(m => new ChatMessage(m.IsUser ? ChatRole.User : ChatRole.Assistant, m.Content)));
        messages.Add(new ChatMessage(ChatRole.User, userMessage));

        var runOptions = new ChatClientAgentRunOptions(new ChatOptions { Tools = tools.AsTools() });
        var response = await agent.RunAsync(messages, options: runOptions, cancellationToken: cancellationToken);

        return new ValuesWizardReply(response.Text, tools.Saved);
    }
}
