namespace CareHub.Billing.Models;

public record InvoiceResponse(
    Guid Id,
    Guid AppointmentId,
    Guid PatientId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    InvoiceStatus Status,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? PaidAt,
    Guid? PaidByUserId,
    DateTime? RefundedAt,
    Guid? RefundedByUserId,
    string? RefundReason)
{
    public static InvoiceResponse FromEntity(Invoice i) => new(
        i.Id,
        i.AppointmentId,
        i.PatientId,
        i.BranchId,
        i.Amount,
        i.Currency,
        i.Status,
        i.CreatedAt,
        i.UpdatedAt,
        i.PaidAt,
        i.PaidByUserId,
        i.RefundedAt,
        i.RefundedByUserId,
        i.RefundReason);
}

public record RefundInvoiceRequest(string Reason);
