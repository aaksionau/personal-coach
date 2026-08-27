using Coach.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Infrastructure.GoogleCalendar;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Google Calendar adapter as the app's <see cref="ICalendarReader"/>. Binds the
    /// <c>"GoogleCalendar"</c> config section; when it's blank the reader simply returns no events.
    /// </summary>
    public static IServiceCollection AddCoachGoogleCalendar(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GoogleCalendarOptions>(configuration.GetSection(GoogleCalendarOptions.SectionName));

        // Stateless and thread-safe (immutable options + a lazily-built, thread-safe CalendarService),
        // so Singleton -- like the AIAgent -- rather than rebuilding the auth stack per Blazor circuit.
        services.AddSingleton<IGoogleCalendarEvents, GoogleCalendarEvents>();
        services.AddSingleton<ICalendarReader, GoogleCalendarReader>();
        return services;
    }
}
