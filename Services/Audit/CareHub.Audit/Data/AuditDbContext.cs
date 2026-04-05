using CareHub.Audit.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Audit.Data;

public class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options) : base(options) { }

    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // True before/after snapshots need richer domain events or synchronous capture in origin services; we persist full event JSON as MVP evidence.
        builder.Entity<AuditLogEntry>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.ActionType).HasMaxLength(128).IsRequired();
            e.Property(x => x.EntityType).HasMaxLength(64);
            e.Property(x => x.DetailsJson).IsRequired();
            e.Property(x => x.BrokerMessageId).HasMaxLength(128);

            e.HasIndex(x => new { x.RecordedAt, x.Id }).IsDescending(true, true);
            e.HasIndex(x => new { x.ActorUserId, x.RecordedAt, x.Id }).IsDescending(false, true, true);
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.RecordedAt, x.Id }).IsDescending(false, false, true, true);
            e.HasIndex(x => new { x.ActionType, x.RecordedAt, x.Id }).IsDescending(false, true, true);

            e.HasIndex(x => x.BrokerMessageId)
                .IsUnique()
                .HasFilter("\"BrokerMessageId\" IS NOT NULL");
        });
    }
}
