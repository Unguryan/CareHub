namespace CareHub.Notification.Messaging;

public sealed class NoOpTelegramNotificationSender : ITelegramNotificationSender
{
    public Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
