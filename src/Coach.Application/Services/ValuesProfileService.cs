using Coach.Application.Interfaces;
using Coach.Domain.Entities;

namespace Coach.Application.Services;

/// <summary>
/// Reads and writes the user's single <see cref="ValuesProfile"/>. This is the public interface the
/// Values Profile feature is tested against -- <see cref="IValuesProfileStore"/> is swapped for a
/// fake in tests, the same way <see cref="ReflectionService"/> is tested against a fake store.
/// Both the guided wizard (via <see cref="Coach.Application.Tools.ValuesProfileTools"/>) and the direct-edit page save
/// through <see cref="SaveProfileAsync"/>, so the "one row, upserted" invariant lives in one place.
/// </summary>
public sealed class ValuesProfileService(IValuesProfileStore valuesProfileStore)
{
    public Task<ValuesProfile?> GetProfileAsync(CancellationToken cancellationToken) =>
        valuesProfileStore.GetAsync(cancellationToken);

    public async Task<ValuesProfile> SaveProfileAsync(string content, CancellationToken cancellationToken)
    {
        // content may be a tool-call argument -- untrusted model output that can arrive null or
        // blank. A validation failure here surfaces as text via ModelToolGuard rather than throwing.
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Values profile content is required.", nameof(content));
        }

        var trimmed = content.Trim();
        var existing = await valuesProfileStore.GetAsync(cancellationToken);
        if (existing is null)
        {
            var created = ValuesProfile.Create(trimmed);
            await valuesProfileStore.AddAsync(created, cancellationToken);
            return created;
        }

        existing.Content = trimmed;
        existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await valuesProfileStore.UpdateAsync(existing, cancellationToken);
        return existing;
    }
}
