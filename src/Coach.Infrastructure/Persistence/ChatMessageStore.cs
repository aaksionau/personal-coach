using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ChatMessageStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IChatMessageStore
{
    public async Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await dbContext.ChatMessages
            .AsNoTracking()
            .Where(m => m.CoachSlug == coachSlug)
            .ToRecentWindowAsync(m => m.CreatedAtUtc, count, cancellationToken);
    }

    public async Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ChatMessages.Add(message);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
