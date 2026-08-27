using Coach.Application.Interfaces;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Coach.Infrastructure.Persistence;

public sealed class ValuesProfileStore(IDbContextFactory<CoachDbContext> dbContextFactory) : IValuesProfileStore
{
    public async Task<ValuesProfile?> GetAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await dbContext.ValuesProfiles.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ValuesProfiles.Add(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ValuesProfile profile, CancellationToken cancellationToken)
    {
        // The read side is AsNoTracking, so the entity arrives detached -- Update marks it for a
        // full-row UPDATE (Content + UpdatedAtUtc are the only fields the service mutates).
        await using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        dbContext.ValuesProfiles.Update(profile);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
