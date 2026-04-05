using CareHub.Notification.Data;
using CareHub.Notification.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using FluentAssertions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Notification.Tests;

public class ConsumerRoutingTests
{
    [Fact]
    public async Task AppointmentCreated_Is_Deduped_On_Redelivery()
    {
        using var factory = new NotificationTestFactory();
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var doctorId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();
        var branchId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var scheduledAt = new DateTime(2026, 6, 1, 10, 0, 0, DateTimeKind.Utc);
        var occurredAt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc);

        var msg = new AppointmentCreated(
            AppointmentId: appointmentId,
            PatientId: patientId,
            DoctorId: doctorId,
            BranchId: branchId,
            ScheduledAt: scheduledAt,
            CreatedByUserId: createdBy,
            OccurredAt: occurredAt);

        using var scope = factory.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publish.Publish(msg);
        await publish.Publish(msg);

        await Task.Delay(500);

        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var dedupes = await db.NotificationDedupes.Where(d => d.DedupeKey == $"AppointmentCreated:{appointmentId}")
            .ToListAsync();
        dedupes.Should().HaveCount(1);

        factory.Telegram.Sent.Should().HaveCount(2);
    }

    [Fact]
    public async Task InvoiceGenerated_Notifies_Accountants_With_Linked_Telegram_Only()
    {
        using var factory = new NotificationTestFactory();
        factory.Identity.BranchRoleResult =
        [
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1"), 301L),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2"), 302L),
            new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb3"), null)
        ];

        var invoiceId = Guid.NewGuid();
        var appointmentId = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var branchId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var occurredAt = DateTime.UtcNow;

        var msg = new InvoiceGenerated(
            InvoiceId: invoiceId,
            AppointmentId: appointmentId,
            PatientId: patientId,
            BranchId: branchId,
            Amount: 100m,
            Currency: "UAH",
            OccurredAt: occurredAt);

        using var scope = factory.Services.CreateScope();
        var publish = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
        await publish.Publish(msg);

        await Task.Delay(500);

        factory.Telegram.Sent.Should().HaveCount(2);
        factory.Telegram.Sent.Select(s => s.ChatId).Should().Contain(new[] { 301L, 302L });
    }
}
