using Azure;
using Azure.AI.OpenAI;
using Coach.Application.Interfaces;
using Coach.Infrastructure.Ai;
using Coach.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Coach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoachInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = OrDefault(configuration.GetConnectionString("CoachDb"), "Host=localhost;Database=coach;Timeout=2");
        services.AddDbContext<CoachDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IChatMessageStore, ChatMessageStore>();

        services.Configure<AzureAiOptions>(configuration.GetSection(AzureAiOptions.SectionName));
        // Falls back to placeholder values when unconfigured, matching the CoachDb connection
        // string above -- lets the app start locally without secrets; the chat page surfaces the
        // resulting call failure rather than the app crashing at startup.
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<AzureAiOptions>>().Value;
            var endpoint = OrDefault(options.Endpoint, "https://unconfigured.invalid");
            var apiKey = OrDefault(options.ApiKey, "unconfigured");
            var deploymentName = OrDefault(options.DeploymentName, "unconfigured");
            return new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey)).GetChatClient(deploymentName);
        });
        services.AddSingleton<IChatCompletionClient, AzureOpenAiChatCompletionClient>();

        return services;
    }

    private static string OrDefault(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value;
}
