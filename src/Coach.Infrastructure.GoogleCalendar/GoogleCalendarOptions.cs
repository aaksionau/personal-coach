namespace Coach.Infrastructure.GoogleCalendar;

/// <summary>
/// Config for the Google Calendar adapter, bound from the <c>"GoogleCalendar"</c> section. The
/// client id/secret come from a one-time Google Cloud OAuth client; the refresh token from a
/// one-time browser authorization the user performs themselves (see docs/google-calendar-setup.md).
/// All blank by default so the app boots without them -- the reader then returns no events.
/// </summary>
internal sealed class GoogleCalendarOptions
{
    public const string SectionName = "GoogleCalendar";

    public string ClientId { get; init; } = string.Empty;

    public string ClientSecret { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    /// <summary>Which calendar to read. "primary" is the signed-in user's own calendar.</summary>
    public string CalendarId { get; init; } = "primary";

    /// <summary>True only when the id/secret/refresh-token needed to mint an access token are all present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId)
        && !string.IsNullOrWhiteSpace(ClientSecret)
        && !string.IsNullOrWhiteSpace(RefreshToken);
}
