namespace CareHub.Notification.Messaging;

public interface ITelegramNotificationSender
{
    Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
