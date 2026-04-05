using CareHub.Laboratory.Data;
using CareHub.Laboratory.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Laboratory.Tests;

public class LabOrderIdempotencyTests : IClassFixture<LaboratoryTestFactory>
{
    private readonly LaboratoryTestFactory _factory;

    public LabOrderIdempotencyTests(LaboratoryTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Duplicate_AppointmentCompleted_Yields_One_LabOrder()
    {
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var branchId = LaboratoryTestFactory.DefaultBranchId;
        var completedAt = new DateTime(2026, 7, 2, 14, 0, 0, DateTimeKind.Utc);
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
        await publish.Publish(msg);
        await harness.InactivityTask.WaitAsync(TimeSpan.FromSeconds(5));

        var db = scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>();
        (await db.LabOrders.CountAsync(o => o.AppointmentId == appointmentId)).Should().Be(1);
    }
}
