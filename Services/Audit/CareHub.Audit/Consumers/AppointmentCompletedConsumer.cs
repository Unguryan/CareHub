using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class AppointmentCompletedConsumer : IConsumer<AppointmentCompleted>
{
    private readonly AuditLogWriter _writer;

    public AppointmentCompletedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<AppointmentCompleted> context) =>
        _writer.WriteAppointmentCompletedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
