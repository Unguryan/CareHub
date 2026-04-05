namespace CareHub.Identity.Services;

public interface ITelegramBotRelay
{
    Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default);
}
