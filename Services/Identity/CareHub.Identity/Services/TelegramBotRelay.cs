using System.Net.Http.Json;

namespace CareHub.Identity.Services;

public sealed class TelegramBotRelay(HttpClient http, IConfiguration configuration) : ITelegramBotRelay
{
    private readonly string _apiKey = configuration["InternalApi:SharedSecret"]
        ?? throw new InvalidOperationException("InternalApi:SharedSecret is not configured.");

    public async Task SendTextAsync(long chatId, string text, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/telegram/send-text");
        request.Headers.TryAddWithoutValidation("X-CareHub-Internal-Key", _apiKey);
        request.Content = JsonContent.Create(new { chatId, text });
        var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
