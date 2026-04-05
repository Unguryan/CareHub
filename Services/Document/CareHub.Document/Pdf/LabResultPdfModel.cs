namespace CareHub.Document.Pdf;

public sealed class LabResultPdfModel
{
    public required Guid LabOrderId { get; init; }
    public required Guid AppointmentId { get; init; }
    public required Guid PatientId { get; init; }
    public required Guid DoctorId { get; init; }
    public string? ResultSummary { get; init; }
    public bool UseFallbackNotice { get; init; }
    public string FallbackMessage { get; init; } = "";
}
