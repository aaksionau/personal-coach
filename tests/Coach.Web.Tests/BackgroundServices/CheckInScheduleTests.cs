using Coach.Web.BackgroundServices;

namespace Coach.Web.Tests.BackgroundServices;

public class CheckInScheduleTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    [Fact]
    public void NextOccurrenceUtc_ReturnsTheComingTargetDayAndTime_LaterThisWeek()
    {
        // Wed 2026-08-26 12:00 UTC; want Monday 08:00 America/Chicago.
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        var next = CheckInSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        // Mon 2026-08-31 08:00 CDT (UTC-5) == 13:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero), next);
        Assert.Equal(DayOfWeek.Monday, TimeZoneInfo.ConvertTime(next, Chicago).DayOfWeek);
    }

    [Fact]
    public void NextOccurrenceUtc_RollsForwardAFullWeek_WhenNowIsPastTodaysTargetTime()
    {
        // Mon 2026-08-31 14:00 CDT (19:00 UTC) -- already past Monday 08:00.
        var now = new DateTimeOffset(2026, 8, 31, 19, 0, 0, TimeSpan.Zero);

        var next = CheckInSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextOccurrenceUtc_RollsForwardAFullWeek_WhenNowIsExactlyTheTargetMoment()
    {
        // Mon 2026-08-31 08:00 CDT == 13:00 UTC, exactly the target.
        var now = new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero);

        var next = CheckInSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextOccurrenceUtc_HonoursTheConfiguredTimeZoneOffset()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        var utc = CheckInSchedule.NextOccurrenceUtc(now, TimeZoneInfo.Utc, DayOfWeek.Monday, new TimeSpan(8, 0, 0));
        var chicago = CheckInSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        // Same wall-clock time, five hours apart in UTC while Chicago is on CDT.
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 8, 0, 0, TimeSpan.Zero), utc);
        Assert.Equal(TimeSpan.FromHours(5), chicago - utc);
    }

    [Fact]
    public void NextOccurrenceUtc_NudgesPastTheSpringForwardGap_RatherThanThrowing()
    {
        // America/Chicago springs forward 2026-03-08: 02:00 -> 03:00 local, so 02:30 does not exist.
        var now = new DateTimeOffset(2026, 3, 8, 6, 0, 0, TimeSpan.Zero); // Sun 2026-03-08 00:00 CST

        var next = CheckInSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Sunday, new TimeSpan(2, 30, 0));

        // Nudged to 03:30 CDT (UTC-5) == 08:30 UTC.
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 8, 30, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextDailyOccurrenceUtc_ReturnsTodaysTime_WhenItIsStillAhead()
    {
        // Mon 2026-08-31 05:00 CDT (10:00 UTC) -- before today's 08:00.
        var now = new DateTimeOffset(2026, 8, 31, 10, 0, 0, TimeSpan.Zero);

        var next = CheckInSchedule.NextDailyOccurrenceUtc(now, Chicago, new TimeSpan(8, 0, 0));

        // Same day, Mon 2026-08-31 08:00 CDT == 13:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextDailyOccurrenceUtc_RollsToTomorrow_WhenNowIsPastTodaysTime()
    {
        // Mon 2026-08-31 14:00 CDT (19:00 UTC) -- already past 08:00.
        var now = new DateTimeOffset(2026, 8, 31, 19, 0, 0, TimeSpan.Zero);

        var next = CheckInSchedule.NextDailyOccurrenceUtc(now, Chicago, new TimeSpan(8, 0, 0));

        // Tue 2026-09-01 08:00 CDT == 13:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 9, 1, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextDailyOccurrenceUtc_RollsToTomorrow_WhenNowIsExactlyTheTargetMoment()
    {
        var now = new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero); // Mon 08:00 CDT exactly

        var next = CheckInSchedule.NextDailyOccurrenceUtc(now, Chicago, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 9, 1, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextDailyOccurrenceUtc_HonoursTheConfiguredTimeZoneOffset()
    {
        var now = new DateTimeOffset(2026, 8, 26, 3, 0, 0, TimeSpan.Zero);

        var utc = CheckInSchedule.NextDailyOccurrenceUtc(now, TimeZoneInfo.Utc, new TimeSpan(8, 0, 0));
        var chicago = CheckInSchedule.NextDailyOccurrenceUtc(now, Chicago, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 8, 26, 8, 0, 0, TimeSpan.Zero), utc);
        Assert.Equal(TimeSpan.FromHours(5), chicago - utc);
    }

    [Fact]
    public void NextDailyOccurrenceUtc_NudgesPastTheSpringForwardGap_RatherThanThrowing()
    {
        // 02:30 local does not exist on 2026-03-08 in America/Chicago.
        var now = new DateTimeOffset(2026, 3, 8, 6, 0, 0, TimeSpan.Zero); // Sun 2026-03-08 00:00 CST

        var next = CheckInSchedule.NextDailyOccurrenceUtc(now, Chicago, new TimeSpan(2, 30, 0));

        Assert.Equal(new DateTimeOffset(2026, 3, 8, 8, 30, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void ResolveTimeZone_ReturnsTheZone_ForAKnownIanaId()
    {
        var resolved = CheckInSchedule.ResolveTimeZone("America/Chicago");

        Assert.Equal(Chicago, resolved);
    }

    [Fact]
    public void ResolveTimeZone_FallsBackToUtcAndReportsTheId_ForAnUnknownZone()
    {
        string? reported = null;

        var resolved = CheckInSchedule.ResolveTimeZone("Mars/Olympus_Mons", id => reported = id);

        Assert.Equal(TimeZoneInfo.Utc, resolved);
        Assert.Equal("Mars/Olympus_Mons", reported);
    }
}
