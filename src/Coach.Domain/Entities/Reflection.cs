namespace Coach.Domain.Entities;

public sealed class Reflection
{
    public required Guid Id { get; init; }

    public required string CoachSlug { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static Reflection Create(string coachSlug, string content) => new()
    {
        Id = Guid.NewGuid(),
        CoachSlug = coachSlug,
        Content = content,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
