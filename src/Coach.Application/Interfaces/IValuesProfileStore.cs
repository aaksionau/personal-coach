using Coach.Domain.Entities;

namespace Coach.Application.Interfaces;

public interface IValuesProfileStore
{
    /// <summary>The single values profile, or <c>null</c> if the wizard has never been completed.</summary>
    Task<ValuesProfile?> GetAsync(CancellationToken cancellationToken);

    Task AddAsync(ValuesProfile profile, CancellationToken cancellationToken);

    Task UpdateAsync(ValuesProfile profile, CancellationToken cancellationToken);
}
