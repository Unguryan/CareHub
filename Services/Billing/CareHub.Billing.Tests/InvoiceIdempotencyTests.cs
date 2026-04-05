using CareHub.Billing.Data;
using CareHub.Billing.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Billing.Tests;

public class InvoiceIdempotencyTests : IClassFixture<BillingTestFactory>
{
    private readonly BillingTestFactory _factory;

    public InvoiceIdempotencyTests(BillingTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Duplicate_AppointmentCompleted_Yields_One_Invoice_And_One_InvoiceGenerated()
    {
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var branchId = BillingTestFactory.DefaultBranchId;
        var completedAt = new DateTime(2026, 7, 2, 14, 0, 0, DateTimeKind.Utc);
        var msg = new AppointmentCompleted(
            AppointmentId: appointmentId,
            PatientId: patientId,
            DoctorId: doctorId,
            BranchId: branchId,
            RequiresLabWork: false,
            CompletedAt: completedAt,
            CompletedByUserId: BillingTestFactory.DefaultUserId,
            OccurredAt: completedAt);

        using var scope = _factory.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();

        await publish.Publish(msg);
        await publish.Publish(msg);
        await harness.InactivityTask.WaitAsync(TimeSpan.FromSeconds(5));

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        (await db.Invoices.CountAsync(i => i.AppointmentId == appointmentId)).Should().Be(1);

        harness.Published.Select<InvoiceGenerated>().Count(m => m.Context.Message.AppointmentId == appointmentId)
            .Should().Be(1);
    }
}
