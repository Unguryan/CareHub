namespace CareHub.Shared.Contracts.Events.Billing;

public record PaymentCompleted(
    Guid InvoiceId,
    Guid PatientId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    Guid ProcessedByUserId,
    DateTime OccurredAt
);
