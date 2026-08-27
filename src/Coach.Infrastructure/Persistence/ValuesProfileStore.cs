using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ValuesProfileStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IValuesProfileStore
{
    public async Task<ValuesProfile?> GetAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ValuesProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == ValuesProfile.SingletonId, cancellationToken);
    }

    public async Task AddAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ValuesProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        // Reads are AsNoTracking, so the entity is detached. Attach and mark only the fields the
        // service mutates -- leaves the immutable Id/CreatedAtUtc out of the UPDATE. Mirrors
        // GoalStore.UpdateActionItemStatusAsync.
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ValuesProfiles.Attach(profile);
        dbContext.Entry(profile).Property(p => p.Content).IsModified = true;
        dbContext.Entry(profile).Property(p => p.UpdatedAtUtc).IsModified = true;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
