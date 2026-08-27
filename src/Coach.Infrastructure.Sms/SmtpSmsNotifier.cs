using Coach.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Sms;

/// <summary>
/// <see cref="ISmsNotifier"/> that delivers a text by SMTP to a Google Fi email-to-SMS gateway
/// address. Unlike the calendar reader (which swallows failures so a coach turn is never blocked),
/// this <b>throws</b> when unconfigured or when the send fails: the only caller is the weekly
/// check-in scheduler, which wants a loud, visible failure in the logs rather than a text that
/// silently never arrives.
/// </summary>
internal sealed class SmtpSmsNotifier(
    ISmtpEmailSender sender,
    IOptions<SmsOptions> options,
    ILogger<SmtpSmsNotifier> logger) : ISmsNotifier
{
    public async Task SendAsync(string message, CancellationToken cancellationToken)
    {
        if (!options.Value.IsConfigured)
        {
            throw new InvalidOperationException(
                "SMS is not configured -- set Sms:SmtpUsername, Sms:SmtpPassword and Sms:ToNumber "
                + "(Sms__SmtpUsername / Sms__SmtpPassword / Sms__ToNumber).");
        }

        var mail = SmsMessageComposer.Compose(options.Value, message);
        using (mail)
        {
            logger.LogInformation("Sending check-in text to {Recipient}.", mail.To.ToString());
            await sender.SendAsync(mail, cancellationToken);
        }
    }
}
