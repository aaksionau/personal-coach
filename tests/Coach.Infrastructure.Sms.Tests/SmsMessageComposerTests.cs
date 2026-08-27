using Coach.Infrastructure.Sms;

namespace Coach.Infrastructure.Sms.Tests;

public class SmsMessageComposerTests
{
    private static SmsOptions OptionsWith(string toNumber = "+1 (555) 123-4567", string gatewayDomain = "msg.fi.google.com") =>
        new()
        {
            SmtpUsername = "coach.bot@gmail.com",
            SmtpPassword = "app-password",
            ToNumber = toNumber,
            GatewayDomain = gatewayDomain,
        };

    [Fact]
    public void Compose_AddressesTheFiGateway_WithADigitsOnlyNumber()
    {
        var mail = SmsMessageComposer.Compose(OptionsWith(), "hello");

        Assert.Equal("15551234567@msg.fi.google.com", Assert.Single(mail.To).Address);
    }

    [Fact]
    public void Compose_SendsFromTheConfiguredGmailAddress()
    {
        var mail = SmsMessageComposer.Compose(OptionsWith(), "hello");

        Assert.Equal("coach.bot@gmail.com", mail.From!.Address);
    }

    [Fact]
    public void Compose_PutsTheMessageInThePlainTextBody_WithNoSubject()
    {
        var mail = SmsMessageComposer.Compose(OptionsWith(), "  Time for your weekly check-in.  ");

        Assert.Equal("Time for your weekly check-in.", mail.Body);
        Assert.Equal(string.Empty, mail.Subject);
        Assert.False(mail.IsBodyHtml);
    }

    [Fact]
    public void Compose_HonoursACustomGatewayDomain()
    {
        var mail = SmsMessageComposer.Compose(OptionsWith(gatewayDomain: "txt.example.net"), "hello");

        Assert.Equal("15551234567@txt.example.net", Assert.Single(mail.To).Address);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Compose_RejectsABlankBody(string body)
    {
        Assert.Throws<ArgumentException>(() => SmsMessageComposer.Compose(OptionsWith(), body));
    }

    [Fact]
    public void Compose_ThrowsWhenTheNumberHasNoDigits()
    {
        Assert.Throws<InvalidOperationException>(
            () => SmsMessageComposer.Compose(OptionsWith(toNumber: "not-a-number"), "hello"));
    }
}
