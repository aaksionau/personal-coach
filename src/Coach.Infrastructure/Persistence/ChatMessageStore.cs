using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ChatMessageStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IChatMessageStore
{
    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

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
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
