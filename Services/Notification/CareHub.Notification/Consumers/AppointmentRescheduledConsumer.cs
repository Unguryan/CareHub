using CareHub.Notification.Models;
using CareHub.Notification.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Notification.Consumers;

public class AppointmentRescheduledConsumer(INotificationOrchestrator orchestrator, ILogger<AppointmentRescheduledConsumer> log)
    : IConsumer<AppointmentRescheduled>
{
    public async Task Consume(ConsumeContext<AppointmentRescheduled> context)
    {
        var m = context.Message;
        var dedupeKey = $"AppointmentRescheduled:{m.AppointmentId}";
        var userIds = new List<Guid> { m.DoctorId };
        var payload = new NotificationPayload(
            "Appointment rescheduled",
            $"Appointment {m.AppointmentId} moved from {m.PreviousScheduledAt:u} to {m.NewScheduledAt:u}.",
            m.OccurredAt,
            "Appointment",
            m.AppointmentId.ToString());
        try
        {
            await orchestrator.HandleAsync(
                dedupeKey, NotificationKind.AppointmentRescheduled, userIds, payload, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Notification orchestration failed for {DedupeKey}", dedupeKey);
        }
    }
}
