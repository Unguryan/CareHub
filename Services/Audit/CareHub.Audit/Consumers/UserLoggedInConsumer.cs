using CareHub.Audit.Services;
using CareHub.Shared.Contracts.Events.Identity;
using MassTransit;

namespace CareHub.Audit.Consumers;

public class UserLoggedInConsumer : IConsumer<UserLoggedIn>
{
    private readonly AuditLogWriter _writer;

    public UserLoggedInConsumer(AuditLogWriter writer) => _writer = writer;

    public Task Consume(ConsumeContext<UserLoggedIn> context) =>
        _writer.WriteUserLoggedInAsync(context.Message, context.MessageId?.ToString(), context.CancellationToken);
}
