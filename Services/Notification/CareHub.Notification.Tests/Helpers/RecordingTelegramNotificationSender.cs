using CareHub.Notification.Messaging;

namespace CareHub.Notification.Tests.Helpers;

public sealed class RecordingTelegramNotificationSender : ITelegramNotificationSender
{
    private readonly object _lock = new();

    public List<(long ChatId, string Text)> Sent { get; } = [];

    public Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        lock (_lock)
            Sent.Add((chatId, text));
        return Task.CompletedTask;
    }
}
