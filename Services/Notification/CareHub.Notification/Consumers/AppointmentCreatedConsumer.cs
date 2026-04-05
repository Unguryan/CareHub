using CareHub.Notification.Models;
using CareHub.Notification.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Notification.Consumers;

public class AppointmentCreatedConsumer(INotificationOrchestrator orchestrator, ILogger<AppointmentCreatedConsumer> log)
    : IConsumer<AppointmentCreated>
{
    public async Task Consume(ConsumeContext<AppointmentCreated> context)
    {
        var m = context.Message;
        var dedupeKey = $"AppointmentCreated:{m.AppointmentId}";
        var userIds = new List<Guid> { m.DoctorId, m.CreatedByUserId };
        var payload = new NotificationPayload(
            "New appointment",
            $"Appointment {m.AppointmentId} scheduled at {m.ScheduledAt:u} for patient {m.PatientId}.",
            m.OccurredAt,
            "Appointment",
            m.AppointmentId.ToString());
        try
        {
            await orchestrator.HandleAsync(
                dedupeKey, NotificationKind.AppointmentCreated, userIds, payload, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Notification orchestration failed for {DedupeKey}", dedupeKey);
        }
    }
}
