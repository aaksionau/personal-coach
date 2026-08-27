using Coach.Infrastructure.Sms;
using Coach.Infrastructure.Sms.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure.Sms.Tests;

public class SmtpSmsNotifierTests
{
    private static readonly SmsOptions Configured = new()
    {
        SmtpUsername = "coach.bot@gmail.com",
        SmtpPassword = "app-password",
        ToNumber = "5551234567",
    };

    [Fact]
    public async Task SendAsync_ComposesAndSendsThroughTheSmtpSender()
    {
        var sender = new FakeSmtpEmailSender();

        await NotifierFor(sender, Configured).SendAsync("Weekly check-in", CancellationToken.None);

        Assert.True(sender.WasCalled);
        Assert.Equal("5551234567@msg.fi.google.com", sender.Recipient);
        Assert.Equal("coach.bot@gmail.com", sender.Sender);
        Assert.Equal("Weekly check-in", sender.Body);
    }

    [Fact]
    public async Task SendAsync_ThrowsAndSkipsTheSender_WhenNotConfigured()
    {
        var sender = new FakeSmtpEmailSender();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NotifierFor(sender, new SmsOptions()).SendAsync("Weekly check-in", CancellationToken.None));

        Assert.False(sender.WasCalled);
    }

    [Fact]
    public async Task SendAsync_PropagatesASendFailure()
    {
        var sender = new FakeSmtpEmailSender { ThrowOnSend = new InvalidOperationException("smtp is down") };

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => NotifierFor(sender, Configured).SendAsync("Weekly check-in", CancellationToken.None));
    }

    private static SmtpSmsNotifier NotifierFor(FakeSmtpEmailSender sender, SmsOptions options) =>
        new(sender, Options.Create(options), NullLogger<SmtpSmsNotifier>.Instance);
}
