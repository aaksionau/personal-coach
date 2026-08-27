namespace Coach.Domain.Entities;

/// <summary>
/// The user's values profile: a single global record (not scoped per coach) holding a distilled
/// summary of what matters to the user, injected into every coach's context. Created once via the
/// guided wizard and edited directly thereafter -- <see cref="Content"/> and
/// <see cref="UpdatedAtUtc"/> are the only mutable fields.
/// </summary>
public sealed class ValuesProfile
{
    /// <summary>
    /// Fixed primary key for the one and only values profile row. Using a constant id makes the
    /// "single global row" invariant a hard database constraint -- a second insert collides on the
    /// primary key rather than silently creating a duplicate.
    /// </summary>
    public static readonly Guid SingletonId = new("0f1b2c3d-0000-0000-0000-000000000001");

    public required Guid Id { get; init; }

    public required string Content { get; set; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; set; }

    public static ValuesProfile Create(string content)
    {
        var now = DateTimeOffset.UtcNow;
        return new ValuesProfile
        {
            Id = SingletonId,
            Content = content,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
