namespace CareHub.Shared.Contracts.Events.Appointments;

public record AppointmentCreated(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId,
    DateTime ScheduledAt,
    Guid CreatedByUserId,
    DateTime OccurredAt
);
