using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Appointment.Events;

public class AppointmentEventPublisher
{
    private readonly IPublishEndpoint _publish;

    public AppointmentEventPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishCreatedAsync(global::CareHub.Appointment.Models.Appointment a, Guid createdByUserId)
        => _publish.Publish(new AppointmentCreated(
            AppointmentId: a.Id,
            PatientId: a.PatientId,
            DoctorId: a.DoctorId,
            BranchId: a.BranchId,
            ScheduledAt: a.ScheduledAt,
            CreatedByUserId: createdByUserId,
            OccurredAt: DateTime.UtcNow));

    public Task PublishCancelledAsync(global::CareHub.Appointment.Models.Appointment a, Guid cancelledByUserId)
        => _publish.Publish(new AppointmentCancelled(
            AppointmentId: a.Id,
            PatientId: a.PatientId,
            DoctorId: a.DoctorId,
            Reason: a.CancellationReason ?? "",
            CancelledByUserId: cancelledByUserId,
            OccurredAt: DateTime.UtcNow));

    public Task PublishRescheduledAsync(
        global::CareHub.Appointment.Models.Appointment a,
        DateTime previousStart,
        Guid rescheduledByUserId)
        => _publish.Publish(new AppointmentRescheduled(
            AppointmentId: a.Id,
            PatientId: a.PatientId,
            DoctorId: a.DoctorId,
            PreviousScheduledAt: previousStart,
            NewScheduledAt: a.ScheduledAt,
            RescheduledByUserId: rescheduledByUserId,
            OccurredAt: DateTime.UtcNow));

    public Task PublishCompletedAsync(global::CareHub.Appointment.Models.Appointment a, Guid completedByUserId)
        => _publish.Publish(new AppointmentCompleted(
            AppointmentId: a.Id,
            PatientId: a.PatientId,
            DoctorId: a.DoctorId,
            BranchId: a.BranchId,
            RequiresLabWork: a.RequiresLabWork,
            CompletedAt: a.CompletedAt ?? DateTime.UtcNow,
            CompletedByUserId: completedByUserId,
            OccurredAt: DateTime.UtcNow));
}
