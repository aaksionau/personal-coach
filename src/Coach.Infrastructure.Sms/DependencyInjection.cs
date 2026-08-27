using Coach.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Infrastructure.Sms;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the SMTP-to-Fi-gateway adapter as the app's <see cref="ISmsNotifier"/>. Binds the
    /// <c>"Sms"</c> config section; when it's blank the notifier throws only when the weekly digest
    /// actually tries to send. Consumed by <c>Coach.Web</c>'s weekly check-in scheduler.
    /// </summary>
    public static IServiceCollection AddCoachSms(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));

        // Stateless (a fresh SmtpClient per send), so Singleton -- like the other adapters.
        services.AddSingleton<ISmtpEmailSender, SmtpEmailSender>();
        services.AddSingleton<ISmsNotifier, SmtpSmsNotifier>();
        return services;
    }
}
