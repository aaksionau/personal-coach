namespace Coach.Application.Interfaces;

/// <summary>
/// Sends one short text message to the user's phone. v1's only implementation delivers it by SMTP
/// to a Google Fi email-to-SMS gateway address (see <c>Coach.Infrastructure.Sms</c>). One-way only:
/// there is no inbound path -- the user always replies by opening the web app, never by texting
/// back -- so this port has no receive method by design.
/// </summary>
public interface ISmsNotifier
{
    /// <summary>
    /// Delivers <paramref name="message"/> as a text. Throws when the notifier isn't configured or
    /// the send fails -- the caller (the weekly check-in scheduler) logs and moves on, the same way
    /// the Garmin CronJob surfaces a loud failure rather than failing silently.
    /// </summary>
    Task SendAsync(string message, CancellationToken cancellationToken);
}
