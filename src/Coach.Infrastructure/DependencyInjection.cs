using Azure;
using Azure.AI.OpenAI;
using Coach.Application.Interfaces;
using Coach.Infrastructure.Options;
using Coach.Infrastructure.Stores;
using Microsoft.Agents.AI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoachInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = OrDefault(configuration.GetConnectionString("CoachDb"), "Host=localhost;Database=coach;Timeout=2");
        // A factory, not a directly-injected scoped context: Blazor Server's DI scope lives for
        // the whole circuit (the browser tab), not one interaction, so a scoped DbContext would
        // accumulate tracked entities for the circuit's lifetime. Each store creates its own
        // short-lived context per call instead.
        services.AddDbContextFactory<CoachDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IChatMessageStore, ChatMessageStore>();
        services.AddScoped<IGoalStore, GoalStore>();
        services.AddScoped<IReflectionStore, ReflectionStore>();
        services.AddScoped<IValuesProfileStore, ValuesProfileStore>();
        services.AddScoped<IGarminMetricsStore, GarminMetricsStore>();

        services.Configure<AzureAiOptions>(configuration.GetSection(AzureAiOptions.SectionName));
        // Falls back to placeholder values when unconfigured, matching the CoachDb connection
        // string above -- lets the app start locally without secrets; the chat page surfaces the
        // resulting call failure rather than the app crashing at startup.
        // No instructions/tools are baked in here: the system prompt varies per turn (goal
        // context changes), and Goal Tracking tools are scoped to a coachSlug per turn -- both are
        // supplied by CoachConversationEngine on each AIAgent.RunAsync call instead.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAiOptions>>().Value;
            var endpoint = OrDefault(options.Endpoint, "https://unconfigured.invalid");
            var apiKey = OrDefault(options.ApiKey, "unconfigured");
            var deploymentName = OrDefault(options.DeploymentName, "unconfigured");
            var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)).GetChatClient(deploymentName);
            // A turn's independent Goal Tracking tool calls (e.g. create a goal plus two action
            // items) don't depend on each other's results, so let the framework run them
            // concurrently instead of its serial-by-default invocation.
            var functionInvokingChatClient = chatClient.AsIChatClient()
                .AsBuilder()
                .UseFunctionInvocation(configure: c => c.AllowConcurrentInvocation = true)
                .Build();
            return (AIAgent)functionInvokingChatClient.AsAIAgent(name: "PersonalCoach");
        });

        return services;
    }

    private static string OrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
