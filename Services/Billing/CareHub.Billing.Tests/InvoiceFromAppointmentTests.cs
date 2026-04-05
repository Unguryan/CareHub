using CareHub.Billing.Data;
using CareHub.Billing.Models;
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

public class InvoiceFromAppointmentTests : IClassFixture<BillingTestFactory>
{
    private readonly BillingTestFactory _factory;

    public InvoiceFromAppointmentTests(BillingTestFactory factory) => _factory = factory;

    [Fact]
    public async Task AppointmentCompleted_Creates_Invoice_And_Publishes_InvoiceGenerated()
    {
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var branchId = BillingTestFactory.DefaultBranchId;
        var completedAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
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
        await publish.Publish(msg);

        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<InvoiceGenerated>()).Should().BeTrue();

        var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
        var row = await db.Invoices.SingleOrDefaultAsync(i => i.AppointmentId == appointmentId);
        row.Should().NotBeNull();
        row!.PatientId.Should().Be(patientId);
        row.BranchId.Should().Be(branchId);
        row.Status.Should().Be(InvoiceStatus.Unpaid);
        row.Amount.Should().Be(500m);
        row.Currency.Should().Be("UAH");
    }
}
