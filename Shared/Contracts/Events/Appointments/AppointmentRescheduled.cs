namespace CareHub.Shared.Contracts.Events.Appointments;

public record AppointmentRescheduled(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    DateTime PreviousScheduledAt,
    DateTime NewScheduledAt,
    Guid RescheduledByUserId,
    DateTime OccurredAt
);
