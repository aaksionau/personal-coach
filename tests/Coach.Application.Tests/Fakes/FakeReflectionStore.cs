using Coach.Application.Interfaces;
using Coach.Domain.Entities;

namespace Coach.Application.Tests.Fakes;

public sealed class FakeReflectionStore : IReflectionStore
{
    private readonly List<Reflection> _reflections = [];

    public IReadOnlyList<Reflection> Added => _reflections;

    private readonly List<string> _requestedCoachSlugs = [];

    /// <summary>Every coach slug <see cref="GetRecentAsync"/> has been called with, in call order -- the builder now reads several coaches' reflections per turn.</summary>
    public IReadOnlyList<string> RequestedCoachSlugs => _requestedCoachSlugs;

    public string? LastRequestedCoachSlug { get; private set; }

    public int? LastRequestedCount { get; private set; }

    public Task AddAsync(Reflection reflection, CancellationToken cancellationToken)
    {
        _reflections.Add(reflection);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Reflection>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken)
    {
        _requestedCoachSlugs.Add(coachSlug);
        LastRequestedCoachSlug = coachSlug;
        LastRequestedCount = count;

        var recent = _reflections
            .Where(r => r.CoachSlug == coachSlug)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Take(count)
            .OrderBy(r => r.CreatedAtUtc)
            .ToList();
        return Task.FromResult<IReadOnlyList<Reflection>>(recent);
    }
}
