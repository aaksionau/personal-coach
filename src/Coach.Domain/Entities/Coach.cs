namespace Coach.Domain.Entities;

/// <summary>A coach identity persisted in the schema (name/prompt/tone live in the Persona Registry, not here).</summary>
public sealed class Coach
{
    public required string Slug { get; init; }

    public required string DisplayName { get; init; }
}
