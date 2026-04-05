using CareHub.Schedule.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Schedule.Data;

public class ScheduleDbContext : DbContext
{
    public ScheduleDbContext(DbContextOptions<ScheduleDbContext> options) : base(options) { }

    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Doctor>(e =>
        {
            e.HasKey(d => d.Id);
            e.Property(d => d.FirstName).IsRequired().HasMaxLength(100);
            e.Property(d => d.LastName).IsRequired().HasMaxLength(100);
            e.Property(d => d.Specialty).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Shift>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Doctor)
                .WithMany()
                .HasForeignKey(s => s.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
            e.Property(s => s.RoomNumber).HasMaxLength(50);
        });
    }
}
