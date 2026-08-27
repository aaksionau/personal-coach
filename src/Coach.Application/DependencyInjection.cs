using Coach.Application.Agents;
using Coach.Application.Builders;
using Coach.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCoachApplication(this IServiceCollection services)
    {
        services.AddSingleton<CoachPersonaRegistry>();
        services.AddScoped<GoalTrackingService>();
        services.AddScoped<ReflectionService>();
        services.AddScoped<ValuesProfileService>();
        services.AddScoped<ValuesWizardService>();
        services.AddScoped<CoachContextBuilder>();
        services.AddScoped<CoachConversationEngine>();
        services.AddScoped<WeeklyDigestService>();
        return services;
    }
}
