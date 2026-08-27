using System.Net.Mail;
using Coach.Infrastructure.Sms;

namespace Coach.Infrastructure.Sms.Tests.Fakes;

/// <summary>
/// Records the message fields at send time -- the notifier disposes the <see cref="MailMessage"/>
/// right after handing it over, so tests read these captured strings, not the live object.
/// </summary>
internal sealed class FakeSmtpEmailSender : ISmtpEmailSender
{
    public Exception? ThrowOnSend { get; set; }

    public bool WasCalled { get; private set; }

    public string? Recipient { get; private set; }

    public string? Sender { get; private set; }

    public string? Body { get; private set; }

    public string? Subject { get; private set; }

    public Task SendAsync(MailMessage message, CancellationToken cancellationToken)
    {
        WasCalled = true;
        Recipient = string.Join(",", message.To.Select(a => a.Address));
        Sender = message.From?.Address;
        Body = message.Body;
        Subject = message.Subject;

        if (ThrowOnSend is not null)
        {
            throw ThrowOnSend;
        }

        return Task.CompletedTask;
    }
}
