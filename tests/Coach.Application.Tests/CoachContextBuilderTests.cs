using Coach.Application.Builders;
using Coach.Application.Models;
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
        var builder = new CoachContextBuilder(
            store,
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("career", context.Persona.Slug);
        Assert.Same(store.MessagesToReturn, context.RecentMessages);
    }

    [Fact]
    public async Task BuildAsync_RequestsRecentMessages_ForTheGivenCoach()
    {
        var store = new FakeChatMessageStore();
        var builder = new CoachContextBuilder(
            store,
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

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
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            goalService,
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Single(context.OwnState.Goals);
        Assert.Equal("Land a staff role", context.OwnState.Goals[0].Goal.Title);
    }

    [Fact]
    public async Task BuildAsync_IncludesTheCoachsRecentReflections_WithoutInvokingAModel()
    {
        var reflectionService = new ReflectionService(new FakeReflectionStore());
        await reflectionService.RecordReflectionAsync("career", "I default to yes under pressure.", CancellationToken.None);
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            reflectionService,
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Single(context.OwnState.Reflections);
        Assert.Equal("I default to yes under pressure.", context.OwnState.Reflections[0].Content);
    }

    [Fact]
    public async Task BuildAsync_IncludesTheOtherCoachesTrackedState_ButNotTheCalledCoachs()
    {
        var goalService = new GoalTrackingService(new FakeGoalStore());
        await goalService.CreateGoalAsync("career", "Land a staff role", CancellationToken.None);
        await goalService.CreateGoalAsync("health", "Sleep 8 hours", CancellationToken.None);

        var reflectionService = new ReflectionService(new FakeReflectionStore());
        await reflectionService.RecordReflectionAsync("health", "I skip workouts when work runs late.", CancellationToken.None);

        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            goalService,
            reflectionService,
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal(
            new[] { "Health Coach", "Kids Coach", "Relationships Coach" },
            context.OtherCoachStates.Select(s => s.CoachName));

        var health = context.OtherCoachStates.Single(s => s.CoachName == "Health Coach");
        Assert.Equal("Sleep 8 hours", Assert.Single(health.Goals).Goal.Title);
        Assert.Equal("I skip workouts when work runs late.", Assert.Single(health.Reflections).Content);
    }

    [Fact]
    public async Task BuildAsync_IncludesTheCurrentValuesProfile_WithoutInvokingAModel()
    {
        var valuesStore = new FakeValuesProfileStore();
        var valuesService = new ValuesProfileService(valuesStore);
        await valuesService.SaveProfileAsync("Autonomy over title; family evenings are protected.", CancellationToken.None);
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            valuesService,
            new FakeCalendarReader(),
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.NotNull(context.ValuesProfile);
        Assert.Equal("Autonomy over title; family evenings are protected.", context.ValuesProfile!.Content);
    }

    [Fact]
    public async Task BuildAsync_IncludesUpcomingCalendarEvents_WithoutInvokingAModel()
    {
        var calendarReader = new FakeCalendarReader
        {
            EventsToReturn =
            [
                new CalendarEvent(
                    "Dentist", DateTimeOffset.UtcNow.AddDays(2), DateTimeOffset.UtcNow.AddDays(2).AddHours(1), IsAllDay: false, Location: null),
            ],
        };
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            calendarReader,
            new FakeGarminMetricsStore());

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Equal("Dentist", Assert.Single(context.UpcomingEvents).Title);
        Assert.Equal(CoachContext.CalendarLookaheadDays, calendarReader.LastRequestedWithinDays);
    }

    [Fact]
    public async Task BuildAsync_IncludesLatestGarminMetrics_ForTheHealthCoach()
    {
        var garminStore = new FakeGarminMetricsStore
        {
            SnapshotToReturn = new GarminMetricsSnapshot(
                new GarminDailyMetric { Date = new DateOnly(2026, 8, 26), Steps = 9000, IngestedAtUtc = DateTimeOffset.UtcNow },
                []),
        };
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            garminStore);

        var context = await builder.BuildAsync("health", CancellationToken.None);

        Assert.NotNull(context.GarminMetrics);
        Assert.Equal(9000, context.GarminMetrics!.Metric.Steps);
    }

    [Fact]
    public async Task BuildAsync_DoesNotTouchGarmin_ForANonHealthCoach()
    {
        var garminStore = new FakeGarminMetricsStore
        {
            SnapshotToReturn = new GarminMetricsSnapshot(
                new GarminDailyMetric { Date = new DateOnly(2026, 8, 26), IngestedAtUtc = DateTimeOffset.UtcNow }, []),
        };
        var builder = new CoachContextBuilder(
            new FakeChatMessageStore(),
            new CoachPersonaRegistry(),
            new GoalTrackingService(new FakeGoalStore()),
            new ReflectionService(new FakeReflectionStore()),
            new ValuesProfileService(new FakeValuesProfileStore()),
            new FakeCalendarReader(),
            garminStore);

        var context = await builder.BuildAsync("career", CancellationToken.None);

        Assert.Null(context.GarminMetrics);
        Assert.False(garminStore.GetLatestWasCalled);
    }
}
