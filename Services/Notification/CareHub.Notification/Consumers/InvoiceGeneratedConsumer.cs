using CareHub.Notification.Clients;
using CareHub.Notification.Models;
using CareHub.Notification.Services;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Notification.Consumers;

public class InvoiceGeneratedConsumer(
    INotificationOrchestrator orchestrator,
    IIdentityInternalClient identity,
    ILogger<InvoiceGeneratedConsumer> log) : IConsumer<InvoiceGenerated>
{
    public async Task Consume(ConsumeContext<InvoiceGenerated> context)
    {
        var m = context.Message;
        var dedupeKey = $"InvoiceGenerated:{m.InvoiceId}";
        try
        {
            var accountants = await identity.GetUsersByBranchAndRoleAsync(m.BranchId, "Accountant", context.CancellationToken);
            var admins = await identity.GetUsersByBranchAndRoleAsync(m.BranchId, "Admin", context.CancellationToken);
            var userIds = accountants.Concat(admins).Select(u => u.UserId).Distinct().ToList();

            var payload = new NotificationPayload(
                "New invoice",
                $"Invoice {m.InvoiceId} for {m.Amount} {m.Currency} (appointment {m.AppointmentId}).",
                m.OccurredAt,
                "Invoice",
                m.InvoiceId.ToString());

            await orchestrator.HandleAsync(
                dedupeKey, NotificationKind.InvoiceGenerated, userIds, payload, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Notification orchestration failed for {DedupeKey}", dedupeKey);
        }
    }
}
