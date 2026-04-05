namespace CareHub.Identity.Services;

public sealed class NoOpTelegramBotRelay : ITelegramBotRelay
{
    public Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
