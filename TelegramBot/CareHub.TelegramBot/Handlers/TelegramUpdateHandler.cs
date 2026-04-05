using CareHub.TelegramBot.Identity;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace CareHub.TelegramBot.Handlers;

public sealed class TelegramUpdateHandler(IdentityLinkClient identity, ILogger<TelegramUpdateHandler> log)
{
    public async Task HandleAsync(ITelegramBotClient bot, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { } message)
            return;

        if (message.Type == MessageType.Text && message.Text?.Trim() == "/start")
        {
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                KeyboardButton.WithRequestContact("Share phone number")
            })
            {
                ResizeKeyboard = true,
                OneTimeKeyboard = true
            };
            await bot.SendMessage(
                message.Chat.Id,
                "Welcome to CareHub. Tap the button below to share your phone number for account linking.",
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
            return;
        }

        if (message.Contact is { PhoneNumber: { } rawPhone })
        {
            var normalized = NormalizePhone(rawPhone);
            var username = message.From?.Username;
            var telegramUserId = message.From?.Id ?? message.Chat.Id;
            try
            {
                var response = await identity.LinkTelegramAsync(telegramUserId, normalized, username, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    await bot.SendMessage(
                        message.Chat.Id,
                        "Your Telegram account is linked. You can remove the keyboard below.",
                        replyMarkup: new ReplyKeyboardRemove(),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    log.LogWarning("Link failed: {Status}", response.StatusCode);
                    await bot.SendMessage(
                        message.Chat.Id,
                        "We could not match that phone number to a CareHub staff account. Contact your administrator.",
                        cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Identity link request failed");
                await bot.SendMessage(
                    message.Chat.Id,
                    "Linking failed temporarily. Please try again later.",
                    cancellationToken: cancellationToken);
            }
        }
    }

    private static string NormalizePhone(string phone)
    {
        var trimmed = phone.Trim().Replace(" ", "", StringComparison.Ordinal);
        return trimmed.StartsWith('+') ? trimmed : "+" + trimmed.TrimStart('+');
    }
}
