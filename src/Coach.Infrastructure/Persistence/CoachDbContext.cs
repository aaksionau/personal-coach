using Coach.Application.Services;
using Microsoft.EntityFrameworkCore;
using CoachEntity = Coach.Domain.Entities.Coach;
using ChatMessageEntity = Coach.Domain.Entities.ChatMessage;

namespace Coach.Infrastructure.Persistence;

public sealed class CoachDbContext(DbContextOptions<CoachDbContext> options) : DbContext(options)
{
    public DbSet<CoachEntity> Coaches => Set<CoachEntity>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoachEntity>(entity =>
        {
            entity.HasKey(c => c.Slug);
            entity.Property(c => c.Slug).HasMaxLength(64);
            entity.Property(c => c.DisplayName).HasMaxLength(128);

            // Seeded from the Persona Registry -- the single source of truth for a coach's
            // slug/display name -- rather than duplicating "career"/"Career Coach" here.
            var seedCoaches = new CoachPersonaRegistry().GetAll()
                .Select(persona => new CoachEntity { Slug = persona.Slug, DisplayName = persona.Name });
            entity.HasData(seedCoaches);
        });

        modelBuilder.Entity<ChatMessageEntity>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.Property(m => m.CoachSlug).HasMaxLength(64);
            entity.Property(m => m.Role).HasConversion<string>().HasMaxLength(16);
            entity.HasOne<CoachEntity>().WithMany().HasForeignKey(m => m.CoachSlug);
            entity.HasIndex(m => new { m.CoachSlug, m.CreatedAtUtc });
        });
    }
}
