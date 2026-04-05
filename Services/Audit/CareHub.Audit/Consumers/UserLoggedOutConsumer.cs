using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Identity;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class UserLoggedOutConsumer : IConsumer<UserLoggedOut>
{
    private readonly AuditLogWriter _writer;

    public UserLoggedOutConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<UserLoggedOut> context) =>
        _writer.WriteUserLoggedOutAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
