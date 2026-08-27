namespace Coach.Web.BackgroundServices;

/// <summary>
/// Config for the ad-hoc due-date nudge trigger, bound from the <c>"Nudge"</c> section. Controls
/// when <see cref="DueDateNudgeScheduler"/> checks for due action items and how wide its window is;
/// the SMS credentials it sends through live in the separate <c>"Sms"</c> section, shared with the
/// weekly digest. Defaults check daily at 08:00 US Central, nudging items due within two days.
/// </summary>
public sealed class NudgeOptions
{
    public const string SectionName = "Nudge";

    /// <summary>Set false to keep the nudge scheduler dormant (e.g. locally, or before SMS is configured).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Local time of day to run the daily check, in <see cref="TimeZoneId"/>.</summary>
    public TimeSpan TimeOfDay { get; init; } = new(8, 0, 0);

    /// <summary>IANA time zone id the schedule is expressed in. Falls back to UTC if unrecognised.</summary>
    public string TimeZoneId { get; init; } = "America/Chicago";

    /// <summary>Nudge an open action item once its due date is within this many days.</summary>
    public int LeadTimeDays { get; init; } = 2;

    /// <summary>Stop nudging an item once it is more than this many days overdue.</summary>
    public int StopAfterOverdueDays { get; init; } = 7;
}
