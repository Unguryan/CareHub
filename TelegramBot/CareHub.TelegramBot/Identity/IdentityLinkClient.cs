using System.Net.Http.Json;

namespace CareHub.TelegramBot.Identity;

public sealed class IdentityLinkClient(HttpClient http)
{
    public Task<HttpResponseMessage> LinkTelegramAsync(
        long telegramUserId,
        string phoneNumber,
        string? telegramUsername,
        CancellationToken cancellationToken = default)
    {
        return http.PostAsJsonAsync(
            "internal/telegram/link",
            new
            {
                telegramUserId,
                phoneNumber,
                telegramUsername
            },
            cancellationToken);
    }
}
