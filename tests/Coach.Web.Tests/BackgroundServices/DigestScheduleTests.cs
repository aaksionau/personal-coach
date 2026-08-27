using Coach.Web.BackgroundServices;

namespace Coach.Web.Tests.BackgroundServices;

public class DigestScheduleTests
{
    private static readonly TimeZoneInfo Chicago = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");

    [Fact]
    public void NextOccurrenceUtc_ReturnsTheComingTargetDayAndTime_LaterThisWeek()
    {
        // Wed 2026-08-26 12:00 UTC; want Monday 08:00 America/Chicago.
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        var next = DigestSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        // Mon 2026-08-31 08:00 CDT (UTC-5) == 13:00 UTC.
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero), next);
        Assert.Equal(DayOfWeek.Monday, TimeZoneInfo.ConvertTime(next, Chicago).DayOfWeek);
    }

    [Fact]
    public void NextOccurrenceUtc_RollsForwardAFullWeek_WhenNowIsPastTodaysTargetTime()
    {
        // Mon 2026-08-31 14:00 CDT (19:00 UTC) -- already past Monday 08:00.
        var now = new DateTimeOffset(2026, 8, 31, 19, 0, 0, TimeSpan.Zero);

        var next = DigestSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextOccurrenceUtc_RollsForwardAFullWeek_WhenNowIsExactlyTheTargetMoment()
    {
        // Mon 2026-08-31 08:00 CDT == 13:00 UTC, exactly the target.
        var now = new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero);

        var next = DigestSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        Assert.Equal(new DateTimeOffset(2026, 9, 7, 13, 0, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void NextOccurrenceUtc_HonoursTheConfiguredTimeZoneOffset()
    {
        var now = new DateTimeOffset(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

        var utc = DigestSchedule.NextOccurrenceUtc(now, TimeZoneInfo.Utc, DayOfWeek.Monday, new TimeSpan(8, 0, 0));
        var chicago = DigestSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Monday, new TimeSpan(8, 0, 0));

        // Same wall-clock time, five hours apart in UTC while Chicago is on CDT.
        Assert.Equal(new DateTimeOffset(2026, 8, 31, 8, 0, 0, TimeSpan.Zero), utc);
        Assert.Equal(TimeSpan.FromHours(5), chicago - utc);
    }

    [Fact]
    public void NextOccurrenceUtc_NudgesPastTheSpringForwardGap_RatherThanThrowing()
    {
        // America/Chicago springs forward 2026-03-08: 02:00 -> 03:00 local, so 02:30 does not exist.
        var now = new DateTimeOffset(2026, 3, 8, 6, 0, 0, TimeSpan.Zero); // Sun 2026-03-08 00:00 CST

        var next = DigestSchedule.NextOccurrenceUtc(now, Chicago, DayOfWeek.Sunday, new TimeSpan(2, 30, 0));

        // Nudged to 03:30 CDT (UTC-5) == 08:30 UTC.
        Assert.Equal(new DateTimeOffset(2026, 3, 8, 8, 30, 0, TimeSpan.Zero), next);
    }

    [Fact]
    public void ResolveTimeZone_ReturnsTheZone_ForAKnownIanaId()
    {
        var resolved = DigestSchedule.ResolveTimeZone("America/Chicago");

        Assert.Equal(Chicago, resolved);
    }

    [Fact]
    public void ResolveTimeZone_FallsBackToUtcAndReportsTheId_ForAnUnknownZone()
    {
        string? reported = null;

        var resolved = DigestSchedule.ResolveTimeZone("Mars/Olympus_Mons", id => reported = id);

        Assert.Equal(TimeZoneInfo.Utc, resolved);
        Assert.Equal("Mars/Olympus_Mons", reported);
    }
}
