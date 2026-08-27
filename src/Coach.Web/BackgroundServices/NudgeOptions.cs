namespace Coach.Web.BackgroundServices;

/// <summary>
/// Config for the ad-hoc due-date nudge trigger, bound from the <c>"Nudge"</c> section. Controls
/// when <see cref="DueDateNudgeScheduler"/> checks for due action items and how wide its window is;
/// the SMS credentials it sends through live in the separate <c>"Sms"</c> section, shared with the
/// weekly digest. Defaults check daily at 08:00 US Central, nudging items due within four days.
/// </summary>
public sealed class NudgeOptions : ScheduleOptions
{
    public const string SectionName = "Nudge";

    /// <summary>Nudge an open action item once its due date is within this many days.</summary>
    public int LeadTimeDays { get; init; } = 4;

    /// <summary>Stop nudging an item once it is more than this many days overdue.</summary>
    public int StopAfterOverdueDays { get; init; } = 7;
}
