using Coach.Application.Interfaces;
using Coach.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddCoachApplication(this IServiceCollection services)
    {
        services.AddSingleton<CoachPersonaRegistry>();
        services.AddScoped<GoalTrackingService>();
        services.AddScoped<IGoalActionExecutor, GoalActionExecutor>();
        services.AddScoped<CoachContextBuilder>();
        services.AddScoped<CoachConversationEngine>();
        return services;
    }
}
