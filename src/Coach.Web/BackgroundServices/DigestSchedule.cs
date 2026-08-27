namespace Coach.Web.BackgroundServices;

/// <summary>
/// The pure "when does the digest next fire" arithmetic, split out of
/// <see cref="WeeklyDigestScheduler"/> so the day-of-week / time-of-day / DST-gap handling is
/// unit-tested without a running host. The scheduler itself stays a thin delay loop over this.
/// </summary>
internal static class DigestSchedule
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

        var unspecified = DateTime.SpecifyKind(localRun, DateTimeKind.Unspecified);
        while (timeZone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone), TimeSpan.Zero);
    }
}
