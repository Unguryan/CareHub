namespace CareHub.Laboratory.Models;

public record LabOrderResponse(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId,
    LabOrderStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? SampleReceivedAt,
    string? ResultSummary,
    DateTime? ResultEnteredAt,
    Guid? ResultEnteredByUserId)
{
    public static LabOrderResponse FromEntity(LabOrder o) => new(
        o.Id,
        o.AppointmentId,
        o.PatientId,
        o.DoctorId,
        o.BranchId,
        o.Status,
        o.CreatedAt,
        o.UpdatedAt,
        o.SampleReceivedAt,
        o.ResultSummary,
        o.ResultEnteredAt,
        o.ResultEnteredByUserId);
}

public record CreateLabOrderRequest(
    Guid AppointmentId,
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId);

public record EnterLabResultRequest(string Summary);
