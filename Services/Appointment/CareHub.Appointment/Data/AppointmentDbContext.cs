using Microsoft.EntityFrameworkCore;

namespace CareHub.Appointment.Data;

public class AppointmentDbContext : DbContext
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options) { }

    public DbSet<global::CareHub.Appointment.Models.Appointment> Appointments =>
        Set<global::CareHub.Appointment.Models.Appointment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<global::CareHub.Appointment.Models.Appointment>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.CancellationReason).HasMaxLength(2000);
            e.HasIndex(a => new { a.DoctorId, a.ScheduledAt, a.Status });
            e.HasIndex(a => a.PatientId);
            e.HasIndex(a => a.BranchId);
        });
    }
}
