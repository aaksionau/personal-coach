using Coach.Application.Agents;
using Coach.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace Coach.Web.BackgroundServices;

/// <summary>
/// The weekly trigger of the Check-in Scheduler: once a week at the configured local day/time it has
/// <see cref="WeeklyDigestService"/> generate one consolidated digest across all four coaches and
/// sends it through <see cref="ISmsNotifier"/>. The delay loop, DST handling and failed-run posture
/// live in <see cref="CheckInScheduler{TOptions}"/>; the model call carries no tests of its own.
/// </summary>
internal sealed class WeeklyDigestScheduler(
    IServiceScopeFactory scopeFactory,
    IOptions<DigestOptions> options,
    ILogger<WeeklyDigestScheduler> logger)
    : CheckInScheduler<DigestOptions>(scopeFactory, options, logger)
{
    protected override string ConfigSectionName => DigestOptions.SectionName;

    protected override string TriggerName => "weekly digest";

    protected override DateTimeOffset NextRun(DateTimeOffset nowUtc, TimeZoneInfo timeZone) =>
        CheckInSchedule.NextOccurrenceUtc(nowUtc, timeZone, Settings.DayOfWeek, Settings.TimeOfDay);

    protected override async Task RunAsync(IServiceScope scope, TimeZoneInfo timeZone, CancellationToken cancellationToken)
    {
        var digestService = scope.ServiceProvider.GetRequiredService<WeeklyDigestService>();
        var smsNotifier = scope.ServiceProvider.GetRequiredService<ISmsNotifier>();

        var digest = await digestService.GenerateAsync(cancellationToken);
        await smsNotifier.SendAsync(digest, cancellationToken);
        Logger.LogInformation("Sent weekly check-in digest ({Length} chars).", digest.Length);
    }
}
