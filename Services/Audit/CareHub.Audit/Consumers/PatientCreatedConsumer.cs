using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Patients;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class PatientCreatedConsumer : IConsumer<PatientCreated>
{
    private readonly AuditLogWriter _writer;

    public PatientCreatedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<PatientCreated> context) =>
        _writer.WritePatientCreatedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
