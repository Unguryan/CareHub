namespace CareHub.Shared.Contracts.Events.Appointments;

public record AppointmentCompleted(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId,
    bool RequiresLabWork,
    DateTime CompletedAt,
    Guid CompletedByUserId,
    DateTime OccurredAt
);
