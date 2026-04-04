using Microsoft.EntityFrameworkCore;
using PatientEntity = CareHub.Patient.Models.Patient;

namespace CareHub.Patient.Data;

public class PatientDbContext : DbContext
{
    public PatientDbContext(DbContextOptions<PatientDbContext> options) : base(options) { }

    public DbSet<PatientEntity> Patients => Set<PatientEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<PatientEntity>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.PhoneNumber).IsUnique();
            e.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
            e.Property(p => p.LastName).IsRequired().HasMaxLength(100);
            e.Property(p => p.PhoneNumber).IsRequired().HasMaxLength(20);
            e.Property(p => p.Email).HasMaxLength(200);
        });
    }
}
