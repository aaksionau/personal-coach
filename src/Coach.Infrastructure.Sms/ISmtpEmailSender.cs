using System.Net.Mail;

namespace Coach.Infrastructure.Sms;

/// <summary>
/// The seam between <see cref="SmtpSmsNotifier"/> and a live SMTP server: sends one already-built
/// <see cref="MailMessage"/>. Split out so the notifier's configured-check and message construction
/// are tested against a fake with no network. Send-only by construction -- there is deliberately no
/// receive method, matching <see cref="Coach.Application.Interfaces.ISmsNotifier"/>.
/// </summary>
internal interface ISmtpEmailSender
{
    Task SendAsync(MailMessage message, CancellationToken cancellationToken);
}
