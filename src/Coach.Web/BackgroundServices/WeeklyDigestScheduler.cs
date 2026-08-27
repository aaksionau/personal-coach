using Coach.Application.Agents;
using Coach.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// The weekly check-in scheduler: once a week at the configured local day/time it has
/// <see cref="WeeklyDigestService"/> generate one consolidated digest across all four coaches and
/// sends it through <see cref="ISmsNotifier"/>. Thin orchestration over already-tested pieces (per
/// the PRD it carries no tests of its own); a failed run is logged and the loop simply waits for
/// next week rather than crashing the app.
/// </summary>
internal sealed class WeeklyDigestScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<DigestOptions> options,
    ILogger<WeeklyDigestScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Weekly digest scheduler is disabled (Digest:Enabled = false).");
            return;
        }

        var timeZone = ResolveTimeZone(settings.TimeZone);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = NextOccurrenceUtc(now, timeZone, settings.DayOfWeek, settings.TimeOfDay);
            logger.LogInformation("Next weekly digest scheduled for {NextRun:u}.", nextRun);

            try
            {
                await Task.Delay(nextRun - now, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await SendDigestAsync(stoppingToken);
        }
    }

    private async Task SendDigestAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var digestService = scope.ServiceProvider.GetRequiredService<WeeklyDigestService>();
            var smsNotifier = scope.ServiceProvider.GetRequiredService<ISmsNotifier>();

            var digest = await digestService.GenerateAsync(cancellationToken);
            await smsNotifier.SendAsync(digest, cancellationToken);
            logger.LogInformation("Sent weekly check-in digest ({Length} chars).", digest.Length);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Weekly check-in digest failed; will retry at the next scheduled time.");
        }
    }

    /// <summary>
    /// The next moment (UTC) matching <paramref name="dayOfWeek"/> at <paramref name="timeOfDay"/>
    /// in <paramref name="timeZone"/>, strictly after <paramref name="nowUtc"/>. Recomputed each
    /// loop so a DST shift in the zone is picked up rather than baked in.
    /// </summary>
    private static DateTimeOffset NextOccurrenceUtc(
        DateTimeOffset nowUtc, TimeZoneInfo timeZone, DayOfWeek dayOfWeek, TimeSpan timeOfDay)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime;
        var daysAhead = ((int)dayOfWeek - (int)localNow.DayOfWeek + 7) % 7;
        var localRun = localNow.Date.AddDays(daysAhead) + timeOfDay;
        if (localRun <= localNow)
        {
            localRun = localRun.AddDays(7);
        }

        var unspecified = DateTime.SpecifyKind(localRun, DateTimeKind.Unspecified);
        // A configured time that lands in this zone's spring-forward gap doesn't exist -- nudge it
        // past the gap rather than letting ConvertTimeToUtc throw and kill the service.
        while (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone), TimeSpan.Zero);
    }

    private TimeZoneInfo ResolveTimeZone(string id)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            logger.LogWarning("Digest:TimeZone '{TimeZone}' not found; scheduling in UTC instead.", id);
            return TimeZoneInfo.Utc;
        }
    }
}
