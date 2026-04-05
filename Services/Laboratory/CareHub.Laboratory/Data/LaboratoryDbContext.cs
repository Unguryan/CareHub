using CareHub.Laboratory.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Laboratory.Data;

public class LaboratoryDbContext : DbContext
{
    public LaboratoryDbContext(DbContextOptions<LaboratoryDbContext> options) : base(options) { }

    public DbSet<LabOrder> LabOrders => Set<LabOrder>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<LabOrder>(e =>
        {
            e.HasKey(o => o.Id);
            e.HasIndex(o => o.AppointmentId).IsUnique();
            e.HasIndex(o => o.PatientId);
            e.HasIndex(o => o.BranchId);
            e.HasIndex(o => o.Status);
            e.HasIndex(o => o.CreatedAt);
            e.Property(o => o.ResultSummary).HasMaxLength(4000);
        });
    }
}
