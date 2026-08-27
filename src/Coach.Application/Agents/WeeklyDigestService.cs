using Coach.Application.Builders;
using Coach.Application.Formatters;
using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Application.Services;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Coach.Application.Agents;

/// <summary>
/// Generates the weekly consolidated check-in digest: assembles goal/reflection state across all
/// four coaches plus the user's calendar and values profile, then has the model write one short
/// text spanning every area -- deliberately model-written, not a template, so it reads like a real
/// check-in. Like <see cref="CoachConversationEngine"/> the model-calling glue here is untested by
/// design; the context assembly it leans on (<see cref="CoachContextBuilder.BuildAllTrackedStatesAsync"/>,
/// <see cref="WeeklyDigestPromptComposer"/>) is covered on its own.
/// </summary>
public sealed class WeeklyDigestService(
    CoachContextBuilder contextBuilder,
    ValuesProfileService valuesProfileService,
    ICalendarReader calendarReader,
    AIAgent agent)
{
    public async Task<string> GenerateAsync(CancellationToken cancellationToken)
    {
        var coachStatesTask = contextBuilder.BuildAllTrackedStatesAsync(cancellationToken);
        var valuesProfileTask = valuesProfileService.GetProfileAsync(cancellationToken);
        var upcomingEventsTask = calendarReader.GetUpcomingEventsAsync(CoachContext.CalendarLookaheadDays, cancellationToken);
        await Task.WhenAll(coachStatesTask, valuesProfileTask, upcomingEventsTask);

        var systemPrompt = WeeklyDigestPromptComposer.Compose(
            coachStatesTask.Result, upcomingEventsTask.Result, valuesProfileTask.Result);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, "Write this week's check-in text."),
        };

        var response = await agent.RunAsync(messages, cancellationToken: cancellationToken);
        return response.Text;
    }
}
