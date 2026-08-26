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
        var builder = new CoachContextBuilder(store, new CoachPersonaRegistry(), new GoalTrackingService(new FakeGoalStore()));

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("career", context.Persona.Slug);
        Assert.Same(store.MessagesToReturn, context.RecentMessages);
    }

    [Fact]
    public async Task BuildAsync_RequestsRecentMessages_ForTheGivenCoach()
    {
        var store = new FakeChatMessageStore();
        var builder = new CoachContextBuilder(store, new CoachPersonaRegistry(), new GoalTrackingService(new FakeGoalStore()));

        await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("career", store.LastRequestedCoachSlug);
        Assert.True(store.LastRequestedCount is > 0);
    }

    [Fact]
    public async Task BuildAsync_IncludesTheCoachsGoals_WithoutInvokingAModel()
    {
        var goalStore = new FakeGoalStore();
        var goalService = new GoalTrackingService(goalStore);
        await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        var builder = new CoachContextBuilder(new FakeChatMessageStore(), new CoachPersonaRegistry(), goalService);

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Single(context.Goals);
        Assert.Equal("Land a staff role", context.Goals[0].Goal.Title);
    }
}
