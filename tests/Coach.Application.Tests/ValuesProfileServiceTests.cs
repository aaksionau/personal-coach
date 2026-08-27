using Coach.Application.Services;
using Coach.Application.Tests.Fakes;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class ValuesProfileServiceTests
{
    [Fact]
    public async Task GetProfileAsync_ReturnsNull_WhenNoProfileHasBeenSaved()
    {
        var service = new ValuesProfileService(new FakeValuesProfileStore());

        Assert.Null(await service.GetProfileAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    [InlineData(null)]
    public async Task SaveProfileAsync_Throws_ForMissingContent(string? content)
    {
        var service = new ValuesProfileService(new FakeValuesProfileStore());

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveProfileAsync(content!, CancellationToken.None));
    }

    [Fact]
    public async Task SaveProfileAsync_CreatesAndTrims_WhenNoProfileExists()
    {
        var store = new FakeValuesProfileStore();
        var service = new ValuesProfileService(store);

        var saved = await service.SaveProfileAsync("  Family first, then craft.  ", CancellationToken.None);

        Assert.Equal("Family first, then craft.", saved.Content);
        Assert.Equal(ValuesProfile.SingletonId, saved.Id);
        Assert.Equal(1, store.AddCount);
        Assert.Equal(0, store.UpdateCount);
        Assert.Equal(saved.Content, (await service.GetProfileAsync(CancellationToken.None))!.Content);
    }

    [Fact]
    public async Task SaveProfileAsync_UpdatesInPlace_WhenAProfileAlreadyExists()
    {
        var store = new FakeValuesProfileStore();
        var service = new ValuesProfileService(store);
        var original = await service.SaveProfileAsync("First draft.", CancellationToken.None);

        var updated = await service.SaveProfileAsync("Second draft.", CancellationToken.None);

        Assert.Equal(original.Id, updated.Id);
        Assert.Equal(original.CreatedAtUtc, updated.CreatedAtUtc);
        Assert.Equal("Second draft.", updated.Content);
        Assert.True(updated.UpdatedAtUtc >= original.UpdatedAtUtc);
        Assert.Equal(1, store.AddCount);
        Assert.Equal(1, store.UpdateCount);
    }
}
