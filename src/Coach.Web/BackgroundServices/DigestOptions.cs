namespace Coach.Web.BackgroundServices;

/// <summary>
/// Config for the weekly check-in digest, bound from the <c>"Digest"</c> section. Controls when
/// <see cref="WeeklyDigestScheduler"/> fires; the SMS credentials it sends through live in the
/// separate <c>"Sms"</c> section. Defaults send Monday 08:00 in US Central time.
/// </summary>
public sealed class DigestOptions
{
    public const string SectionName = "Digest";

    /// <summary>Set false to keep the scheduler dormant (e.g. locally, or before SMS is configured).</summary>
    public bool Enabled { get; init; } = true;

    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Monday;

    /// <summary>Local time of day to send, in <see cref="TimeZoneId"/>.</summary>
    public TimeSpan TimeOfDay { get; init; } = new(8, 0, 0);

    /// <summary>IANA time zone id the schedule is expressed in. Falls back to UTC if unrecognised.</summary>
    public string TimeZoneId { get; init; } = "America/Chicago";
}
