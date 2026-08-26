using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Coach.Domain.Entities;
using Coach.Domain.Enums;

namespace Coach.Application.Tests;

public class CoachContextBuilderTests
{
    [Fact]
    public async Task BuildAsync_ReturnsPersonaAndRecentMessages_WithoutInvokingAModel()
    {
        var store = new FakeChatMessageStore
        {
            MessagesToReturn =
            [
                new ChatMessage
                {
                    Id = Guid.NewGuid(),
                    CoachSlug = "career",
                    Role = ChatMessageRole.User,
                    Content = "How's my resume look?",
                    CreatedAtUtc = DateTimeOffset.UtcNow,
                },
            ],
        };
        var builder = new CoachContextBuilder(store, new CoachPersonaRegistry());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("career", context.Persona.Slug);
        Assert.Same(store.MessagesToReturn, context.RecentMessages);
    }

    [Fact]
    public async Task BuildAsync_RequestsRecentMessages_ForTheGivenCoach()
    {
        var store = new FakeChatMessageStore();
        var builder = new CoachContextBuilder(store, new CoachPersonaRegistry());

        await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("career", store.LastRequestedCoachSlug);
        Assert.True(store.LastRequestedCount is > 0);
    }
}
