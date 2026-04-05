using CareHub.Document.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Document.Data;

public class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<StoredDocument> StoredDocuments => Set<StoredDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<StoredDocument>();
        e.ToTable("StoredDocuments");
        e.HasKey(x => x.Id);
        e.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        e.Property(x => x.ContentType).HasMaxLength(128).IsRequired();
        e.Property(x => x.StorageKey).HasMaxLength(512).IsRequired();
        e.Property(x => x.Sha256).HasMaxLength(64);
        e.Property(x => x.EntityType).HasMaxLength(64).IsRequired();
        e.HasIndex(x => new { x.Kind, x.EntityId }).IsUnique();
        e.HasIndex(x => x.EntityType);
        e.HasIndex(x => x.CreatedAt);
    }
}
