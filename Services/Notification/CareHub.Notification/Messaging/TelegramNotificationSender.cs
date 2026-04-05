using Telegram.Bot;

namespace CareHub.Notification.Messaging;

public sealed class TelegramNotificationSender(ITelegramBotClient bot, ILogger<TelegramNotificationSender> log)
    : ITelegramNotificationSender
{
    public async Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            await bot.SendMessage(chatId, text, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Telegram send failed for chat {ChatId}", chatId);
            throw;
        }
    }
}
