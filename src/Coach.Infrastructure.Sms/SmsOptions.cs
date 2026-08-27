namespace Coach.Infrastructure.Sms;

/// <summary>
/// Config for the SMS notifier, bound from the <c>"Sms"</c> section. Delivery is by SMTP through a
/// Gmail account to a Google Fi email-to-SMS gateway address (<c>{number}@{GatewayDomain}</c>) --
/// the same shape as weather-home-station's <c>SmsNotifier</c>. <see cref="SmtpUsername"/> is a
/// Gmail address (also used as the From) and <see cref="SmtpPassword"/> a Gmail app password (not
/// the account password); <see cref="ToNumber"/> is the destination Fi number. All blank by default
/// so the app still boots without them -- the notifier then throws when the weekly digest actually
/// tries to send (see docs/sms-notifier-setup.md). Unlike the weather worker there is no
/// <c>Enabled</c> flag here: the schedule's own <c>Digest:Enabled</c> is the single on/off switch.
/// </summary>
internal sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string SmtpHost { get; init; } = "smtp.gmail.com";

    public int SmtpPort { get; init; } = 587;

    /// <summary>The Gmail address to authenticate with; also the address the text is sent from.</summary>
    public string SmtpUsername { get; init; } = string.Empty;

    /// <summary>A Gmail app password for <see cref="SmtpUsername"/> -- not the account password.</summary>
    public string SmtpPassword { get; init; } = string.Empty;

    /// <summary>The destination phone number. Non-digits are stripped before addressing.</summary>
    public string ToNumber { get; init; } = string.Empty;

    /// <summary>The Google Fi email-to-SMS gateway domain. The default is Fi's current gateway.</summary>
    public string GatewayDomain { get; init; } = "msg.fi.google.com";

    /// <summary>True only when the username, app password, and destination number are all present.</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SmtpUsername)
        && !string.IsNullOrWhiteSpace(SmtpPassword)
        && !string.IsNullOrWhiteSpace(ToNumber);
}
