namespace Coach.Web.BackgroundServices;

/// <summary>
/// The pure "when does the next check-in fire" arithmetic, split out of the schedulers so the
/// day-of-week / time-of-day / DST-gap handling is unit-tested without a running host. Backs both
/// triggers of the Check-in Scheduler: <see cref="WeeklyDigestScheduler"/> (weekly, via
/// <see cref="NextOccurrenceUtc"/>) and <see cref="DueDateNudgeScheduler"/> (daily, via
/// <see cref="NextDailyOccurrenceUtc"/>). Each scheduler stays a thin delay loop over this.
/// </summary>
internal static class CheckInSchedule
{
    /// <summary>
    /// Resolves an IANA (or Windows) time-zone id to a <see cref="TimeZoneInfo"/>, falling back to
    /// UTC when the id is unknown or malformed. <paramref name="onFallback"/> is invoked with the
    /// unresolved id when that happens, so the caller can log it.
    /// </summary>
    internal static TimeZoneInfo ResolveTimeZone(string timeZoneId, Action<string>? onFallback = null)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            onFallback?.Invoke(timeZoneId);
            return TimeZoneInfo.Utc;
        }
    }

    /// <summary>
    /// The next moment (UTC) matching <paramref name="dayOfWeek"/> at <paramref name="timeOfDay"/>
    /// in <paramref name="timeZone"/>, strictly after <paramref name="nowUtc"/>. Recomputed each
    /// loop so a DST shift in the zone is picked up rather than baked in; a configured time that
    /// lands in this zone's spring-forward gap is nudged past the gap rather than throwing.
    /// </summary>
    internal static DateTimeOffset NextOccurrenceUtc(
        DateTimeOffset nowUtc, TimeZoneInfo timeZone, DayOfWeek dayOfWeek, TimeSpan timeOfDay)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime;
        var daysAhead = ((int)dayOfWeek - (int)localNow.DayOfWeek + 7) % 7;
        var localRun = localNow.Date.AddDays(daysAhead) + timeOfDay;
        if (localRun <= localNow)
        {
            localRun = localRun.AddDays(7);
        }

        return ToUtcPastAnyGap(localRun, timeZone);
    }

    /// <summary>
    /// The next moment (UTC) at <paramref name="timeOfDay"/> in <paramref name="timeZone"/>,
    /// strictly after <paramref name="nowUtc"/> -- today's occurrence if it is still ahead,
    /// otherwise tomorrow's. Same DST-gap handling as <see cref="NextOccurrenceUtc"/>.
    /// </summary>
    internal static DateTimeOffset NextDailyOccurrenceUtc(
        DateTimeOffset nowUtc, TimeZoneInfo timeZone, TimeSpan timeOfDay)
    {
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, timeZone).DateTime;
        var localRun = localNow.Date + timeOfDay;
        if (localRun <= localNow)
        {
            localRun = localRun.AddDays(1);
        }

        return ToUtcPastAnyGap(localRun, timeZone);
    }

    private static DateTimeOffset ToUtcPastAnyGap(DateTime localRun, TimeZoneInfo timeZone)
    {
        var unspecified = DateTime.SpecifyKind(localRun, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone), TimeSpan.Zero);
    }
}
