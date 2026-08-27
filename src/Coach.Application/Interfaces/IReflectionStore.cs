using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

public interface IReflectionStore
{
    Task AddAsync(Reflection reflection, CancellationToken cancellationToken);

    /// <summary>Most recent reflections for a coach, oldest first, limited to <paramref name="count"/>.</summary>
    Task<IReadOnlyList<Reflection>> GetRecentAsync(string coachSlug, int count, CancellationToken cancellationToken);
}
