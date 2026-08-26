namespace Coach.Domain.Entities;

public sealed class Goal
{
    public required Guid Id { get; init; }

    public required string CoachSlug { get; init; }

    public required string Title { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static Goal Create(string coachSlug, string title) => new()
    {
        Id = Guid.NewGuid(),
        CoachSlug = coachSlug,
        Title = title,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
