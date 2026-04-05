// EF migrations (repo root):
// dotnet ef migrations add <Name> --project Services/Reporting/CareHub.Reporting --startup-project Services/Reporting/CareHub.Reporting
// dotnet ef database update --project Services/Reporting/CareHub.Reporting --startup-project Services/Reporting/CareHub.Reporting

using CareHub.Reporting.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Reporting.Data;

public class ReportingDbContext : DbContext
{
    public ReportingDbContext(DbContextOptions<ReportingDbContext> options)
        : base(options) { }

    public DbSet<ReportPatientFact> ReportPatientFacts => Set<ReportPatientFact>();
    public DbSet<ReportAppointmentFact> ReportAppointmentFacts => Set<ReportAppointmentFact>();
    public DbSet<ReportPaymentFact> ReportPaymentFacts => Set<ReportPaymentFact>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ReportPatientFact>(e =>
        {
            e.HasKey(x => x.PatientId);
            e.HasIndex(x => x.BranchId);
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<ReportAppointmentFact>(e =>
        {
            e.HasKey(x => x.AppointmentId);
            e.HasIndex(x => x.BranchId);
            e.HasIndex(x => x.DoctorId);
            e.HasIndex(x => x.ScheduledAt);
            e.HasIndex(x => x.CompletedAt);
            e.HasIndex(x => x.CancelledAt);
            e.Property(x => x.CancellationReason).HasMaxLength(2000);
        });

        modelBuilder.Entity<ReportPaymentFact>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.MessageId).IsUnique();
            e.HasIndex(x => x.BranchId);
            e.HasIndex(x => x.OccurredAt);
            e.HasIndex(x => x.InvoiceId);
            e.Property(x => x.Currency).HasMaxLength(16);
        });
    }
}
