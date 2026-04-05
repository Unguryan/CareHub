using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class PaymentCompletedConsumer : IConsumer<PaymentCompleted>
{
    private readonly AuditLogWriter _writer;

    public PaymentCompletedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<PaymentCompleted> context) =>
        _writer.WritePaymentCompletedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
