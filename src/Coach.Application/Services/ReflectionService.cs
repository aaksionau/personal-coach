using Coach.Application.Interfaces;
using Coach.Domain.Entities;

namespace Coach.Application.Services;

/// <summary>
/// Records and retrieves Reflections for a coach -- a reflection is a distinct record capturing an
/// insight or theme drawn from check-ins over time. This is the public interface Reflections is
/// tested against; <see cref="IReflectionStore"/> is swapped for a fake in tests, the same way
/// <see cref="GoalTrackingService"/> is tested against a fake goal store.
/// </summary>
public sealed class ReflectionService(IReflectionStore reflectionStore)
{
    public async Task<Reflection> RecordReflectionAsync(string coachSlug, string content, CancellationToken cancellationToken)
    {
        // content is a tool-call argument -- untrusted model output that can arrive null or blank.
        // A validation failure here surfaces as text via ModelToolGuard rather than throwing.
        var trimmedContent = (content ?? string.Empty).Trim();
        if (trimmedContent.Length == 0)
        {
            throw new ArgumentException("Reflection content is required.", nameof(content));
        }

        var reflection = Reflection.Create(coachSlug, trimmedContent);
        await reflectionStore.AddAsync(reflection, cancellationToken);
        return reflection;
    }

    public Task<IReadOnlyList<Reflection>> GetRecentReflectionsAsync(
        string coachSlug, int count, CancellationToken cancellationToken) =>
        reflectionStore.GetRecentAsync(coachSlug, count, cancellationToken);
}
