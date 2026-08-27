using global::Garmin.Connect.Models;

namespace Coach.Infrastructure.Garmin;

/// <summary>
/// The raw Garmin Connect responses for one day, as returned by <see cref="IGarminApi"/> --
/// the input to <see cref="GarminDailyMetricMapper"/>. Holds SDK types directly (like
/// <c>GoogleCalendarEventMapper</c> taking a Google <c>Events</c>), so the mapping is exercised
/// against real response shapes with no network. <see cref="Summary"/> / <see cref="Sleep"/> are
/// nullable because Garmin returns nothing for a day it has no data for.
/// </summary>
internal sealed record GarminDaySnapshot(
    GarminStats? Summary,
    GarminSleepData? Sleep,
    IReadOnlyList<GarminActivity> Activities);
