using CareHub.Laboratory.Data;
using CareHub.Laboratory.Models;
using CareHub.Laboratory.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Laboratory.Tests;

public class LabOrderFromAppointmentTests : IClassFixture<LaboratoryTestFactory>
{
    private readonly LaboratoryTestFactory _factory;

    public LabOrderFromAppointmentTests(LaboratoryTestFactory factory) => _factory = factory;

    [Fact]
    public async Task AppointmentCompleted_WithLabWork_Creates_Pending_Order()
    {
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var branchId = LaboratoryTestFactory.DefaultBranchId;
        var completedAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        var msg = new AppointmentCompleted(
            AppointmentId: appointmentId,
            PatientId: patientId,
            DoctorId: doctorId,
            BranchId: branchId,
            RequiresLabWork: true,
            CompletedAt: completedAt,
            CompletedByUserId: LaboratoryTestFactory.DefaultUserId,
            OccurredAt: completedAt);

        using var scope = _factory.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        await publish.Publish(msg);
        await harness.InactivityTask.WaitAsync(TimeSpan.FromSeconds(5));

        var db = scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>();
        var row = await db.LabOrders.SingleOrDefaultAsync(o => o.AppointmentId == appointmentId);
        row.Should().NotBeNull();
        row!.PatientId.Should().Be(patientId);
        row.DoctorId.Should().Be(doctorId);
        row.BranchId.Should().Be(branchId);
        row.Status.Should().Be(LabOrderStatus.Pending);
    }

    [Fact]
    public async Task AppointmentCompleted_WithoutLabWork_Does_Not_Create_Order()
    {
        var appointmentId = Guid.NewGuid();
        var completedAt = new DateTime(2026, 7, 1, 13, 0, 0, DateTimeKind.Utc);
        var msg = new AppointmentCompleted(
            AppointmentId: appointmentId,
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            BranchId: LaboratoryTestFactory.DefaultBranchId,
            RequiresLabWork: false,
            CompletedAt: completedAt,
            CompletedByUserId: LaboratoryTestFactory.DefaultUserId,
            OccurredAt: completedAt);

        using var scope = _factory.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        await publish.Publish(msg);
        await harness.InactivityTask.WaitAsync(TimeSpan.FromSeconds(5));

        var db = scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>();
        (await db.LabOrders.AnyAsync(o => o.AppointmentId == appointmentId)).Should().BeFalse();
    }
}
