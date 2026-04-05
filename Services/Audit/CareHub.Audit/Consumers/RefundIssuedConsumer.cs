using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class RefundIssuedConsumer : IConsumer<RefundIssued>
{
    private readonly AuditLogWriter _writer;

    public RefundIssuedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<RefundIssued> context) =>
        _writer.WriteRefundIssuedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
