using CareHub.Notification.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Notification.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationDedupe> NotificationDedupes => Set<NotificationDedupe>();

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationDedupe>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.DedupeKey).IsUnique();
        });

        modelBuilder.Entity<NotificationDelivery>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.CreatedAt);
            e.HasIndex(x => x.TargetUserId);
            e.HasIndex(x => x.Kind);
            e.HasIndex(x => x.DedupeKey);
        });
    }
}
