using Coach.Application.Interfaces;
using Coach.Domain.Entities;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeValuesProfileStore : IValuesProfileStore
{
    private ValuesProfile? _profile;

    public int AddCount { get; private set; }

    public int UpdateCount { get; private set; }

    public Task<ValuesProfile?> GetAsync(CancellationToken cancellationToken) => Task.FromResult(_profile);

    public Task AddAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        AddCount++;
        _profile = profile;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        UpdateCount++;
        _profile = profile;
        return Task.CompletedTask;
    }
}
