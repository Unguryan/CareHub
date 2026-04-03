namespace CareHub.Shared.Contracts.Events.Billing;

public record RefundIssued(
    Guid InvoiceId,
    Guid PatientId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    string Reason,
    Guid IssuedByUserId,
    DateTime OccurredAt
);
