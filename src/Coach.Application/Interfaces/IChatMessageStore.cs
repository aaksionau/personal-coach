using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

public interface IChatMessageStore
{
    /// <summary>Most recent messages for a coach, oldest first, limited to <paramref name="count"/>.</summary>
    Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken);

    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
}
