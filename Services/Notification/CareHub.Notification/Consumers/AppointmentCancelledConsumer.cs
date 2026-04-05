using CareHub.Notification.Models;
using CareHub.Notification.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Notification.Consumers;

public class AppointmentCancelledConsumer(INotificationOrchestrator orchestrator, ILogger<AppointmentCancelledConsumer> log)
    : IConsumer<AppointmentCancelled>
{
    public async Task Consume(ConsumeContext<AppointmentCancelled> context)
    {
        var m = context.Message;
        var dedupeKey = $"AppointmentCancelled:{m.AppointmentId}";
        var userIds = new List<Guid> { m.DoctorId };
        var payload = new NotificationPayload(
            "Appointment cancelled",
            $"Appointment {m.AppointmentId} was cancelled. Reason: {m.Reason}",
            m.OccurredAt,
            "Appointment",
            m.AppointmentId.ToString());
        try
        {
            await orchestrator.HandleAsync(
                dedupeKey, NotificationKind.AppointmentCancelled, userIds, payload, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Notification orchestration failed for {DedupeKey}", dedupeKey);
        }
    }
}
