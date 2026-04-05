namespace CareHub.Document.Clients;

public sealed record LaboratoryDocumentContextDto(
    Guid LabOrderId,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid? BranchId,
    string? ResultSummary,
    DateTime? ResultEnteredAt);
