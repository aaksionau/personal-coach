using Coach.Domain.Enums;

namespace Coach.Domain.Entities;

public sealed class ChatMessage
{
    public required Guid Id { get; init; }

    public required string CoachSlug { get; init; }

    public required ChatMessageRole Role { get; init; }

    public required string Content { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public static ChatMessage Create(string coachSlug, ChatMessageRole role, string content) => new()
    {
        Id = Guid.NewGuid(),
        CoachSlug = coachSlug,
        Role = role,
        Content = content,
        CreatedAtUtc = DateTimeOffset.UtcNow,
    };
}
