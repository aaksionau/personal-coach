using Coach.Application.Interfaces;
using Coach.Domain.Entities;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeChatMessageStore : IChatMessageStore
{
    public List<ChatMessage> Added { get; } = [];

    public IReadOnlyList<ChatMessage> MessagesToReturn { get; set; } = [];

    public string? LastRequestedCoachSlug { get; private set; }

    public int? LastRequestedCount { get; private set; }

    public Task<IReadOnlyList<ChatMessage>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        LastRequestedCoachSlug = coachSlug;
        LastRequestedCount = count;
        return Task.FromResult(MessagesToReturn);
    }

    public Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        Added.Add(message);
        return Task.CompletedTask;
    }
}
