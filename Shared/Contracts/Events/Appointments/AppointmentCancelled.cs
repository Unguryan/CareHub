namespace CareHub.Shared.Contracts.Events.Appointments;

public record AppointmentCancelled(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    string Reason,
    Guid CancelledByUserId,
    DateTime OccurredAt
);
