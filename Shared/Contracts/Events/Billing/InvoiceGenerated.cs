namespace CareHub.Shared.Contracts.Events.Billing;

public record InvoiceGenerated(
    Guid InvoiceId,
    Guid AppointmentId,
    Guid PatientId,
    Guid BranchId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt
);
