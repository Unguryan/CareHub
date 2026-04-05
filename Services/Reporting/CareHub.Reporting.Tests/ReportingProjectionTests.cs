using CareHub.Reporting.Data;
using CareHub.Reporting.Services;
using CareHub.Reporting.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Patients;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Reporting.Tests;

[Collection("Reporting")]
public class ReportingProjectionTests
{
    private readonly ReportingTestFactory _factory;

    public ReportingProjectionTests(ReportingTestFactory factory) => _factory = factory;

    [Fact]
    public async Task PatientCreated_Idempotent_OnDuplicateApply()
    {
        using var scope = _factory.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<ReportingProjectionService>();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var patientId = Guid.NewGuid();
        var msg = new PatientCreated(
            patientId,
            "A",
            "B",
            "+100",
            ReportingTestFactory.DefaultBranchId,
            ReportingTestFactory.DefaultUserId,
            new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc));

        await projection.ApplyPatientCreatedAsync(msg, default);
        await projection.ApplyPatientCreatedAsync(msg, default);

        (await db.ReportPatientFacts.CountAsync(p => p.PatientId == patientId)).Should().Be(1);
    }

    [Fact]
    public async Task AppointmentCreatedThenCompleted_MergesIntoSingleFact()
    {
        using var scope = _factory.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<ReportingProjectionService>();

        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var branchId = ReportingTestFactory.DefaultBranchId;
        var scheduledAt = new DateTime(2026, 5, 1, 9, 0, 0, DateTimeKind.Utc);
        var completedAt = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc);

        await projection.ApplyAppointmentCreatedAsync(
            new AppointmentCreated(
                appointmentId,
                patientId,
                doctorId,
                branchId,
                scheduledAt,
                ReportingTestFactory.DefaultUserId,
                scheduledAt),
            default);

        await projection.ApplyAppointmentCompletedAsync(
            new AppointmentCompleted(
                appointmentId,
                patientId,
                doctorId,
                branchId,
                false,
                completedAt,
                ReportingTestFactory.DefaultUserId,
                completedAt),
            default);

        using var readScope = _factory.Services.CreateScope();
        var db = readScope.ServiceProvider.GetRequiredService<ReportingDbContext>();
        var row = await db.ReportAppointmentFacts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);
        row.Should().NotBeNull();
        row!.ScheduledAt.Should().Be(scheduledAt);
        row.CompletedAt.Should().Be(completedAt);
        row.BranchId.Should().Be(branchId);
    }

    [Fact]
    public async Task PaymentCompleted_Idempotent_ForSameDedupeKey()
    {
        using var scope = _factory.Services.CreateScope();
        var projection = scope.ServiceProvider.GetRequiredService<ReportingProjectionService>();
        var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();

        var msg = new PaymentCompleted(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ReportingTestFactory.DefaultBranchId,
            50m,
            "USD",
            ReportingTestFactory.DefaultUserId,
            new DateTime(2026, 6, 1, 8, 0, 0, DateTimeKind.Utc));

        await projection.ApplyPaymentCompletedAsync(msg, "dedupe-test-key", default);
        await projection.ApplyPaymentCompletedAsync(msg, "dedupe-test-key", default);

        (await db.ReportPaymentFacts.CountAsync(p => p.MessageId == "dedupe-test-key")).Should().Be(1);
    }
}
