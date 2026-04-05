using CareHub.Billing.Models;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Billing.Events;

public class BillingEventPublisher
{
    private readonly IPublishEndpoint _publish;

    public BillingEventPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishInvoiceGeneratedAsync(Invoice invoice, DateTime occurredAt)
        => _publish.Publish(new InvoiceGenerated(
            InvoiceId: invoice.Id,
            AppointmentId: invoice.AppointmentId,
            PatientId: invoice.PatientId,
            BranchId: invoice.BranchId,
            Amount: invoice.Amount,
            Currency: invoice.Currency,
            OccurredAt: occurredAt));

    public Task PublishPaymentCompletedAsync(Invoice invoice, Guid processedByUserId, DateTime occurredAt)
        => _publish.Publish(new PaymentCompleted(
            InvoiceId: invoice.Id,
            PatientId: invoice.PatientId,
            BranchId: invoice.BranchId,
            Amount: invoice.Amount,
            Currency: invoice.Currency,
            ProcessedByUserId: processedByUserId,
            OccurredAt: occurredAt));

    public Task PublishRefundIssuedAsync(Invoice invoice, Guid issuedByUserId, string reason, DateTime occurredAt)
        => _publish.Publish(new RefundIssued(
            InvoiceId: invoice.Id,
            PatientId: invoice.PatientId,
            BranchId: invoice.BranchId,
            Amount: invoice.Amount,
            Currency: invoice.Currency,
            Reason: reason,
            IssuedByUserId: issuedByUserId,
            OccurredAt: occurredAt));
}
