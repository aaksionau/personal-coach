using Azure;
using Azure.AI.OpenAI;
using Coach.Application.Interfaces;
using Coach.Infrastructure.Ai;
using Coach.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Coach.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoachInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("CoachDb");
        services.AddDbContext<CoachDbContext>(options =>
            options.UseNpgsql(string.IsNullOrWhiteSpace(connectionString)
                ? "Host=localhost;Database=coach;Timeout=2"
                : connectionString));
        services.AddScoped<IChatMessageStore, ChatMessageStore>();

        var azureAiOptions = configuration.GetSection(AzureAiOptions.SectionName).Get<AzureAiOptions>() ?? new AzureAiOptions();
        services.AddSingleton(azureAiOptions);
        // Falls back to placeholder values when unconfigured, matching the CoachDb connection
        // string above -- lets the app start locally without secrets; the chat page surfaces the
        // resulting call failure rather than the app crashing at startup.
        var endpoint = string.IsNullOrWhiteSpace(azureAiOptions.Endpoint) ? "https://unconfigured.invalid" : azureAiOptions.Endpoint;
        var apiKey = string.IsNullOrWhiteSpace(azureAiOptions.ApiKey) ? "unconfigured" : azureAiOptions.ApiKey;
        var deploymentName = string.IsNullOrWhiteSpace(azureAiOptions.DeploymentName) ? "unconfigured" : azureAiOptions.DeploymentName;
        services.AddSingleton(_ =>
            new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey))
                .GetChatClient(deploymentName));
        services.AddSingleton<IChatCompletionClient, AzureOpenAiChatCompletionClient>();

        return services;
    }
}
