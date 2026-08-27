using Coach.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Infrastructure.Garmin;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Garmin Connect adapter as the app's <see cref="IGarminMetricsReader"/>. Binds
    /// the <c>"Garmin"</c> config section; when it's blank the collector throws only when actually
    /// asked to collect a day. Consumed by the standalone Garmin ingestion CronJob, not the web app.
    /// </summary>
    public static IServiceCollection AddCoachGarminIngestion(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GarminOptions>(configuration.GetSection(GarminOptions.SectionName));

        // Stateless bar an in-memory session token cached inside the SDK client, so Singleton --
        // like the calendar adapter -- rather than rebuilding the auth stack per use.
        services.AddSingleton<IGarminApi, LiveGarminApi>();
        services.AddSingleton<IGarminMetricsReader, GarminMetricsCollector>();
        return services;
    }
}
