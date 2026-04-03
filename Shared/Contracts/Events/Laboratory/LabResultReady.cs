namespace CareHub.Shared.Contracts.Events.Laboratory;

public record LabResultReady(
    Guid LabOrderId,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid LabTechnicianId,
    DateTime OccurredAt
);
