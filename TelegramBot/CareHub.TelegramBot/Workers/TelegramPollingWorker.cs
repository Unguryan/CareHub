using CareHub.TelegramBot.Handlers;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace CareHub.TelegramBot.Workers;

public sealed class TelegramPollingWorker(
    TelegramBotClient bot,
    TelegramUpdateHandler handler,
    ILogger<TelegramPollingWorker> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var receiverOptions = new ReceiverOptions { AllowedUpdates = [] };

        async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken ct) =>
            await handler.HandleAsync(client, update, ct);

        Task HandleErrorAsync(ITelegramBotClient botClient, Exception ex, CancellationToken ct) =>
            Task.Run(() => log.LogError(ex, "Telegram polling error"), ct);

        await bot.ReceiveAsync(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandleErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken);
    }
}
