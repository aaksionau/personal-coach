namespace Coach.Web.BackgroundServices;

/// <summary>
/// The schedule fields shared by both Check-in Scheduler triggers -- whether the trigger is live,
/// and when (and in which zone) it runs. <see cref="DigestOptions"/> and <see cref="NudgeOptions"/>
/// each add their trigger-specific settings on top, and <see cref="CheckInScheduler{TOptions}"/> is
/// generic over this base.
/// </summary>
public abstract class ScheduleOptions
{
    /// <summary>Set false to keep the scheduler dormant (e.g. locally, or before SMS is configured).</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Local time of day to run, in <see cref="TimeZoneId"/>.</summary>
    public TimeSpan TimeOfDay { get; init; } = new(8, 0, 0);

    /// <summary>IANA time zone id the schedule is expressed in. Falls back to UTC if unrecognised.</summary>
    public string TimeZoneId { get; init; } = "America/Chicago";
}
