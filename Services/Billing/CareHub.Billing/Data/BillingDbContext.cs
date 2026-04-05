using CareHub.Billing.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Billing.Data;

public class BillingDbContext : DbContext
{
    public BillingDbContext(DbContextOptions<BillingDbContext> options) : base(options) { }

    public DbSet<Invoice> Invoices => Set<Invoice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Invoice>(e =>
        {
            e.HasKey(i => i.Id);
            e.HasIndex(i => i.AppointmentId).IsUnique();
            e.HasIndex(i => i.PatientId);
            e.HasIndex(i => i.BranchId);
            e.HasIndex(i => i.Status);
            e.HasIndex(i => i.CreatedAt);
            e.Property(i => i.Currency).HasMaxLength(8);
            e.Property(i => i.RefundReason).HasMaxLength(2000);
        });
    }
}
