using Coach.Application.Services;
using Coach.Application.Tests.Fakes;

namespace Coach.Application.Tests;

public class ReflectionServiceTests
{
    [Fact]
    public async Task RecordReflectionAsync_PersistsATrimmedReflection_ScopedToTheCoach()
    {
        var store = new FakeReflectionStore();
        var service = new ReflectionService(store);

        var reflection = await service.RecordReflectionAsync(
            "career", "  I keep avoiding hard feedback conversations.  ", CancellationToken.None);

        Assert.Equal("career", reflection.CoachSlug);
        Assert.Equal("I keep avoiding hard feedback conversations.", reflection.Content);
        Assert.Single(store.Added);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task RecordReflectionAsync_ThrowsArgumentException_ForMissingContent(string? content)
    {
        var service = new ReflectionService(new FakeReflectionStore());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RecordReflectionAsync("career", content!, CancellationToken.None));
    }

    [Fact]
    public async Task GetRecentReflectionsAsync_ReturnsCoachReflections_OldestFirst()
    {
        var store = new FakeReflectionStore();
        var service = new ReflectionService(store);
        await service.RecordReflectionAsync("career", "First", CancellationToken.None);
        await Task.Delay(5);
        await service.RecordReflectionAsync("career", "Second", CancellationToken.None);
        await service.RecordReflectionAsync("health", "Other coach", CancellationToken.None);

        var reflections = await service.GetRecentReflectionsAsync("career", 10, CancellationToken.None);

        Assert.Equal(["First", "Second"], reflections.Select(r => r.Content));
        Assert.Equal("career", store.LastRequestedCoachSlug);
        Assert.Equal(10, store.LastRequestedCount);
    }
}
