using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Sms;

/// <summary>
/// <see cref="ISmtpEmailSender"/> backed by <see cref="SmtpClient"/> over Gmail's submission port
/// (587, STARTTLS) authenticated with the Gmail app password. A fresh client per send -- the weekly
/// digest sends at most once a week, so there is nothing to pool.
/// </summary>
internal sealed class SmtpEmailSender(IOptions<SmsOptions> options) : ISmtpEmailSender
{
    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword),
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
