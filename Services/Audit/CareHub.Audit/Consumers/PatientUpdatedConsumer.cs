using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Patients;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class PatientUpdatedConsumer : IConsumer<PatientUpdated>
{
    private readonly AuditLogWriter _writer;

    public PatientUpdatedConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<PatientUpdated> context) =>
        _writer.WritePatientUpdatedAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
