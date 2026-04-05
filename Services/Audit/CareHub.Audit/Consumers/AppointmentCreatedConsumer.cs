using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class AppointmentCreatedConsumer : IConsumer<AppointmentCreated>
{
    private readonly AuditLogWriter _writer;

    public AppointmentCreatedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<AppointmentCreated> context) =>
        _writer.WriteAppointmentCreatedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
