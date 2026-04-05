using CareHub.Notification.Models;
using CareHub.Notification.Services;
using CareHub.Shared.Contracts.Events.Laboratory;
using MassTransit;

namespace CareHub.Notification.Consumers;

public class LabResultReadyConsumer(INotificationOrchestrator orchestrator, ILogger<LabResultReadyConsumer> log)
    : IConsumer<LabResultReady>
{
    public async Task Consume(ConsumeContext<LabResultReady> context)
    {
        var m = context.Message;
        var dedupeKey = $"LabResultReady:{m.LabOrderId}";
        var userIds = new List<Guid> { m.DoctorId, m.LabTechnicianId };
        var payload = new NotificationPayload(
            "Lab result ready",
            $"Lab order {m.LabOrderId} (appointment {m.AppointmentId}) has a new result.",
            m.OccurredAt,
            "LabOrder",
            m.LabOrderId.ToString());
        try
        {
            await orchestrator.HandleAsync(
                dedupeKey, NotificationKind.LabResultReady, userIds, payload, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Notification orchestration failed for {DedupeKey}", dedupeKey);
        }
    }
}
