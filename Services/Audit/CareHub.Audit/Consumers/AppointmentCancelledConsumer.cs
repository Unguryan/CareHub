using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class AppointmentCancelledConsumer : IConsumer<AppointmentCancelled>
{
    private readonly AuditLogWriter _writer;

    public AppointmentCancelledConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<AppointmentCancelled> context) =>
        _writer.WriteAppointmentCancelledAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
