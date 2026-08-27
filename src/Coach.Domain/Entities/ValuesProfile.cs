namespace Coach.Domain.Entities;

/// <summary>
/// The user's values profile: a single global record (not scoped per coach) holding a distilled
/// summary of what matters to the user, injected into every coach's context. Created once via the
/// guided wizard and edited directly thereafter -- <see cref="Content"/> and
/// <see cref="UpdatedAtUtc"/> are the only mutable fields.
/// </summary>
public sealed class ValuesProfile
{
    public required Guid Id { get; init; }

    public required string Content { get; set; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; set; }

    public static ValuesProfile Create(string content)
    {
        var now = DateTimeOffset.UtcNow;
        return new ValuesProfile
        {
            Id = Guid.NewGuid(),
            Content = content,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
        };
    }
}
