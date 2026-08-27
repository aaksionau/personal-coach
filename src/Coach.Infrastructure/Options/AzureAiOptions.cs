namespace Coach.Infrastructure.Options;

public sealed class AzureAiOptions
{
    public const string SectionName = "AzureAi";

    public string Endpoint { get; init; } = string.Empty;

    public string ApiKey { get; init; } = string.Empty;

    public string DeploymentName { get; init; } = string.Empty;
}
