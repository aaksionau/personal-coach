using Coach.Application.Agents;
using Coach.Application.Interfaces;
using Coach.Application.Models;
using Coach.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// The ad-hoc due-date nudge trigger of the Check-in Scheduler: once a day at the configured local
/// time it asks <see cref="IDueDateNudgeStore"/> for open action items whose due date is close (or
/// overdue), has <see cref="DueDateNudgeService"/> write a short model nudge grounded in each one,
/// and sends it through the same <see cref="ISmsNotifier"/> the weekly digest uses. Thin
/// orchestration over already-tested pieces (the next-fire arithmetic lives in
/// <see cref="CheckInSchedule"/>, which is tested; the delay loop and model call carry no tests).
/// A failed item is logged and left unrecorded so it retries tomorrow; a failed run is logged and
/// the loop simply waits for the next day rather than crashing the app.
/// </summary>
internal sealed class DueDateNudgeScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<NudgeOptions> options,
    ILogger<DueDateNudgeScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Due-date nudge scheduler is disabled (Nudge:Enabled = false).");
            return;
        }

        var timeZone = CheckInSchedule.ResolveTimeZone(
            settings.TimeZoneId,
            id => logger.LogWarning("Nudge:TimeZoneId '{TimeZoneId}' not found; scheduling in UTC instead.", id));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CheckInSchedule.NextDailyOccurrenceUtc(now, timeZone, settings.TimeOfDay);
            logger.LogInformation("Next due-date nudge check scheduled for {NextRun:u}.", nextRun);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunNudgesAsync(settings, timeZone, stoppingToken);
        }
    }

    private async Task RunNudgesAsync(NudgeOptions settings, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var nudgeStore = scope.ServiceProvider.GetRequiredService<IDueDateNudgeStore>();
            var nudgeService = scope.ServiceProvider.GetRequiredService<DueDateNudgeService>();
            var smsNotifier = scope.ServiceProvider.GetRequiredService<ISmsNotifier>();

            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, timeZone).DateTime);
            var pending = await nudgeStore.GetPendingNudgesAsync(
                today, settings.LeadTimeDays, settings.StopAfterOverdueDays, cancellationToken);

            if (pending.Count == 0)
            {
                logger.LogInformation("No action items due a nudge today.");
                return;
            }

            var sent = 0;
            foreach (var item in pending)
            {
                if (await TrySendNudgeAsync(item, today, nudgeService, smsNotifier, nudgeStore, cancellationToken))
                {
                    sent++;
                }
            }

            logger.LogInformation("Sent {Sent} of {Total} due-date nudge(s).", sent, pending.Count);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Due-date nudge run failed; will retry at the next scheduled time.");
        }
    }

    private async Task<bool> TrySendNudgeAsync(
        PendingNudge item,
        DateOnly today,
        DueDateNudgeService nudgeService,
        ISmsNotifier smsNotifier,
        IDueDateNudgeStore nudgeStore,
        CancellationToken cancellationToken)
    {
        var actionItem = item.ActionItem;
        try
        {
            var text = await nudgeService.GenerateAsync(item, today, cancellationToken);
            await smsNotifier.SendAsync(text, cancellationToken);
            await nudgeStore.RecordNudgeSentAsync(
                DueDateNudge.Create(actionItem.Id, today, actionItem.DueDate!.Value), cancellationToken);
            logger.LogInformation(
                "Sent due-date nudge for action item {ActionItemId} ({Length} chars).",
                actionItem.Id, text.Length);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Left unrecorded on purpose: the item resurfaces in tomorrow's run.
            logger.LogError(ex, "Failed to send due-date nudge for action item {ActionItemId}.", actionItem.Id);
            return false;
        }
    }
}
