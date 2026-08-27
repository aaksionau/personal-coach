using System.Net.Mail;

namespace Coach.Infrastructure.Sms;

/// <summary>
/// Builds the <see cref="MailMessage"/> for a text: a message from the configured Gmail address to
/// the Fi gateway address <c>{digits-only number}@{gateway domain}</c>, with the digest as the
/// plain-text body and no subject (the gateway drops it into the SMS otherwise). Pure and split out
/// from <see cref="SmtpSmsNotifier"/> so recipient/message construction is unit-tested without
/// sending any mail.
/// </summary>
internal static class SmsMessageComposer
{
    public static MailMessage Compose(SmsOptions options, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("SMS message body is required.", nameof(message));
        }

        var digits = DigitsOnly(options.ToNumber);
        if (digits.Length == 0)
        {
            throw new InvalidOperationException(
                $"Sms:ToNumber ('{options.ToNumber}') contains no digits to address a text to.");
        }

        return new MailMessage(options.SmtpUsername, $"{digits}@{options.GatewayDomain}")
        {
            Subject = string.Empty,
            Body = message.Trim(),
            IsBodyHtml = false,
        };
    }

    private static string DigitsOnly(string value) =>
        new(value.Where(char.IsAsciiDigit).ToArray());
}
