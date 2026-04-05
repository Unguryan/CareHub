using System.Net.Http.Json;
using System.Text.Json;

namespace CareHub.Notification.Clients;

public sealed class IdentityInternalClient(HttpClient http) : IIdentityInternalClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<long?> GetTelegramChatIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using var response = await http.GetAsync($"internal/users/{userId}/telegram", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<TelegramUserResponseDto>(JsonOptions, cancellationToken);
        return dto?.TelegramChatId;
    }

    public async Task<IReadOnlyList<IdentityUserTelegramRow>> GetUsersByBranchAndRoleAsync(
        Guid branchId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var url = $"internal/users/by-branch-role?branchId={branchId}&role={Uri.EscapeDataString(role)}";
        var list = await http.GetFromJsonAsync<List<IdentityUserTelegramRow>>(url, JsonOptions, cancellationToken);
        return list ?? [];
    }

    private sealed record TelegramUserResponseDto(long TelegramChatId);
}
