namespace Coach.Web.BackgroundServices;

/// <summary>
/// Config for the weekly check-in digest, bound from the <c>"Digest"</c> section. Controls when
/// <see cref="WeeklyDigestScheduler"/> fires; the SMS credentials it sends through live in the
/// separate <c>"Sms"</c> section. Defaults send Monday 08:00 in US Central time.
/// </summary>
public sealed class DigestOptions : ScheduleOptions
{
    public const string SectionName = "Digest";

    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Monday;
}
