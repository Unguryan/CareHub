using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class AppointmentRescheduledConsumer : IConsumer<AppointmentRescheduled>
{
    private readonly AuditLogWriter _writer;

    public AppointmentRescheduledConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<AppointmentRescheduled> context) =>
        _writer.WriteAppointmentRescheduledAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
