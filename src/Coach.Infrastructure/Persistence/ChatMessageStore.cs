using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ChatMessageStore(CoachDbContext dbContext) : IChatMessageStore
{
    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        var recentDescending = await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.CoachSlug == coachSlug)
            .OrderByDescending(m => m.CreatedAtUtc)
            .Take(count)
            .ToListAsync(cancellationToken);

        recentDescending.Reverse();
        return recentDescending;
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
