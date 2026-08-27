using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// Shared shell for the Check-in Scheduler's two sibling triggers (<see cref="WeeklyDigestScheduler"/>,
/// <see cref="DueDateNudgeScheduler"/>): the enabled check, time-zone resolution, and the resilient
/// delay loop that recomputes the next fire each iteration (so a DST shift in the zone is picked up),
/// sleeps to it, then runs the trigger body inside a fresh DI scope with a catch that logs a failed
/// run and waits for the next one rather than crashing the app. Subclasses supply only their
/// schedule expression (<see cref="NextRun"/>) and their per-run work (<see cref="RunAsync"/>). The
/// pure next-fire arithmetic lives in <see cref="CheckInSchedule"/>, which is tested; this loop and
/// the model calls it drives carry no tests, by repo convention.
/// </summary>
internal abstract class CheckInScheduler<TOptions> : BackgroundService
    where TOptions : ScheduleOptions
{
    private readonly IServiceScopeFactory scopeFactory;

    protected CheckInScheduler(IServiceScopeFactory scopeFactory, IOptions<TOptions> options, ILogger logger)
    {
        this.scopeFactory = scopeFactory;
        Settings = options.Value;
        Logger = logger;
    }

    protected TOptions Settings { get; }

    protected ILogger Logger { get; }

    /// <summary>Config section name, used only in log messages (e.g. "Digest", "Nudge").</summary>
    protected abstract string ConfigSectionName { get; }

    /// <summary>Human-readable trigger name for log messages (e.g. "weekly digest", "due-date nudge").</summary>
    protected abstract string TriggerName { get; }

    /// <summary>The next moment this trigger should fire, strictly after <paramref name="nowUtc"/>.</summary>
    protected abstract DateTimeOffset NextRun(DateTimeOffset nowUtc, TimeZoneInfo timeZone);

    /// <summary>Do the trigger's work for one fire, resolving services from <paramref name="scope"/>.</summary>
    protected abstract Task RunAsync(IServiceScope scope, TimeZoneInfo timeZone, CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Settings.Enabled)
        {
            Logger.LogInformation(
                "{Trigger} scheduler is disabled ({Section}:Enabled = false).", TriggerName, ConfigSectionName);
            return;
        }

        var timeZone = CheckInSchedule.ResolveTimeZone(
            Settings.TimeZoneId,
            id => Logger.LogWarning(
                "{Section}:TimeZoneId '{TimeZoneId}' not found; scheduling in UTC instead.", ConfigSectionName, id));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = NextRun(now, timeZone);
            Logger.LogInformation("Next {Trigger} run scheduled for {NextRun:u}.", TriggerName, nextRun);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunGuardedAsync(timeZone, stoppingToken);
        }
    }

    private async Task RunGuardedAsync(TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            await RunAsync(scope, timeZone, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "{Trigger} run failed; will retry at the next scheduled time.", TriggerName);
        }
    }
}
