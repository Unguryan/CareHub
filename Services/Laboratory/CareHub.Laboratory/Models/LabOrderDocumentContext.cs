namespace CareHub.Laboratory.Models;

public record LabOrderDocumentContext(
    Guid LabOrderId,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid? BranchId,
    string? ResultSummary,
    DateTime? ResultEnteredAt);
