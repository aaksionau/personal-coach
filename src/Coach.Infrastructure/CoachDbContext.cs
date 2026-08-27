using Coach.Application.Services;
using Coach.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using CoachEntity = Coach.Domain.Entities.Coach;
using ChatMessageEntity = Coach.Domain.Entities.ChatMessage;

namespace Coach.Infrastructure;

public sealed class CoachDbContext(DbContextOptions<CoachDbContext> options, CoachPersonaRegistry personaRegistry) : DbContext(options)
{
    public DbSet<CoachEntity> Coaches => Set<CoachEntity>();

    public DbSet<ChatMessageEntity> ChatMessages => Set<ChatMessageEntity>();

    public DbSet<Goal> Goals => Set<Goal>();

    public DbSet<ActionItem> ActionItems => Set<ActionItem>();

    public DbSet<Reflection> Reflections => Set<Reflection>();

    // Bookkeeping for the ad-hoc due-date nudge trigger: one row per action item per day nudged.
    public DbSet<DueDateNudge> DueDateNudges => Set<DueDateNudge>();

    // Global, not coach-scoped (no CoachSlug/FK); single row keyed by ValuesProfile.SingletonId.
    // Id is the PK by convention and Content maps to unbounded text, so no OnModelCreating block.
    public DbSet<ValuesProfile> ValuesProfiles => Set<ValuesProfile>();

    // Written by the standalone Garmin ingestion CronJob, read by the Health coach's context.
    public DbSet<GarminDailyMetric> GarminDailyMetrics => Set<GarminDailyMetric>();

    public DbSet<GarminActivitySummary> GarminActivitySummaries => Set<GarminActivitySummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CoachEntity>(entity =>
        {
            entity.HasKey(c => c.Slug);
            entity.Property(c => c.Slug).HasMaxLength(64);
            entity.Property(c => c.DisplayName).HasMaxLength(128);

            // Seeded from the Persona Registry -- the single source of truth for a coach's
            // slug/display name -- rather than duplicating "career"/"Career Coach" here.
            var seedCoaches = personaRegistry.GetAll()
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

        modelBuilder.Entity<Goal>(entity =>
        {
            entity.HasKey(g => g.Id);
            entity.Property(g => g.CoachSlug).HasMaxLength(64);
            entity.Property(g => g.Title).HasMaxLength(256);
            entity.HasOne<CoachEntity>().WithMany().HasForeignKey(g => g.CoachSlug);
            entity.HasIndex(g => g.CoachSlug);
        });

        modelBuilder.Entity<ActionItem>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.Property(a => a.Description).HasMaxLength(512);
            entity.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
            entity.HasOne<Goal>().WithMany().HasForeignKey(a => a.GoalId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => a.GoalId);
        });

        modelBuilder.Entity<Reflection>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CoachSlug).HasMaxLength(64);
            // Content is free-form model-generated prose -- left as unbounded text, matching
            // ChatMessage.Content, so an over-long value can't throw past ModelToolGuard.
            entity.HasOne<CoachEntity>().WithMany().HasForeignKey(r => r.CoachSlug);
            entity.HasIndex(r => new { r.CoachSlug, r.CreatedAtUtc });
        });

        modelBuilder.Entity<DueDateNudge>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.HasOne<ActionItem>().WithMany().HasForeignKey(n => n.ActionItemId).OnDelete(DeleteBehavior.Cascade);
            // One nudge per action item per local calendar day -- the dedup guarantee the scheduler leans on.
            entity.HasIndex(n => new { n.ActionItemId, n.NudgeDate }).IsUnique();
        });

        modelBuilder.Entity<GarminDailyMetric>(entity =>
        {
            // One row per calendar day; the ingestion job replaces a day's row to re-ingest it.
            entity.HasKey(m => m.Date);
        });

        modelBuilder.Entity<GarminActivitySummary>(entity =>
        {
            entity.HasKey(a => a.ActivityId);
            // ActivityId is Garmin's own id, not a generated key.
            entity.Property(a => a.ActivityId).ValueGeneratedNever();
            entity.Property(a => a.Name).HasMaxLength(256);
            entity.Property(a => a.ActivityType).HasMaxLength(64);
            entity.HasOne<GarminDailyMetric>().WithMany().HasForeignKey(a => a.Date).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(a => a.Date);
        });
    }
}
