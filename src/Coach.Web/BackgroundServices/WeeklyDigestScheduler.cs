using Coach.Application.Agents;
using Coach.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// The weekly check-in scheduler: once a week at the configured local day/time it has
/// <see cref="WeeklyDigestService"/> generate one consolidated digest across all four coaches and
/// sends it through <see cref="ISmsNotifier"/>. Thin orchestration over already-tested pieces (the
/// next-fire arithmetic lives in <see cref="CheckInSchedule"/>, which is tested; the delay loop and
/// model call carry no tests of their own); a failed run is logged and the loop simply waits for
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

        var timeZone = CheckInSchedule.ResolveTimeZone(
            settings.TimeZoneId,
            id => logger.LogWarning("Digest:TimeZoneId '{TimeZoneId}' not found; scheduling in UTC instead.", id));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTimeOffset.UtcNow;
            var nextRun = CheckInSchedule.NextOccurrenceUtc(now, timeZone, settings.DayOfWeek, settings.TimeOfDay);
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
}
