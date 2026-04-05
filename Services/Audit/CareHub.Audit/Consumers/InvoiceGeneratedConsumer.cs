using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class InvoiceGeneratedConsumer : IConsumer<InvoiceGenerated>
{
    private readonly AuditLogWriter _writer;

    public InvoiceGeneratedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<InvoiceGenerated> context) =>
        _writer.WriteInvoiceGeneratedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
