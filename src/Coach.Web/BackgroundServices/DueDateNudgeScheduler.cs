using Coach.Application.Agents;
using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// The ad-hoc due-date trigger of the Check-in Scheduler: once a day at the configured local time it
/// asks <see cref="IDueDateNudgeStore"/> for open action items whose due date is close (or overdue),
/// has <see cref="DueDateNudgeService"/> write a short model nudge grounded in each one, and sends it
/// through the same <see cref="ISmsNotifier"/> the weekly digest uses. Each item's generate + send +
/// record is wrapped so one model or SMS failure doesn't block the rest of the run; a failed item is
/// left unrecorded so it resurfaces tomorrow. The delay loop and failed-run posture live in
/// <see cref="CheckInScheduler{TOptions}"/>.
/// </summary>
internal sealed class DueDateNudgeScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<NudgeOptions> options,
    ILogger<DueDateNudgeScheduler> logger)
    : CheckInScheduler<NudgeOptions>(scopeFactory, options, logger)
{
    protected override string ConfigSectionName => NudgeOptions.SectionName;

    protected override string TriggerName => "due-date nudge";

    protected override DateTimeOffset NextRun(DateTimeOffset nowUtc, TimeZoneInfo timeZone) =>
        CheckInSchedule.NextDailyOccurrenceUtc(nowUtc, timeZone, Settings.TimeOfDay);

    protected override async Task RunAsync(IServiceScope scope, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var nudgeStore = scope.ServiceProvider.GetRequiredService<IDueDateNudgeStore>();
        var nudgeService = scope.ServiceProvider.GetRequiredService<DueDateNudgeService>();
        var smsNotifier = scope.ServiceProvider.GetRequiredService<ISmsNotifier>();

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
        var pending = await nudgeStore.GetPendingNudgesAsync(
            today, Settings.LeadTimeDays, Settings.StopAfterOverdueDays, cancellationToken);

        if (pending.Count == 0)
        {
            Logger.LogInformation("No action items due a nudge today.");
            return;
        }

        var sent = 0;
        foreach (var item in pending)
        {
            if (await TrySendNudgeAsync(item))
            {
                sent++;
            }
        }

        Logger.LogInformation("Sent {Sent} of {Total} due-date nudge(s).", sent, pending.Count);

        async Task<bool> TrySendNudgeAsync(PendingNudge item)
        {
            try
            {
                var text = await nudgeService.GenerateAsync(item, today, cancellationToken);
                await smsNotifier.SendAsync(text, cancellationToken);
                await nudgeStore.RecordNudgeSentAsync(
                    DueDateNudge.Create(item.ActionItemId, today, item.DueDate), cancellationToken);
                Logger.LogInformation(
                    "Sent due-date nudge for action item {ActionItemId} ({Length} chars).", item.ActionItemId, text.Length);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Left unrecorded on purpose: the item resurfaces in tomorrow's run.
                Logger.LogError(ex, "Failed to send due-date nudge for action item {ActionItemId}.", item.ActionItemId);
                return false;
            }
        }
    }
}
