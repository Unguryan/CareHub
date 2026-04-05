using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CareHub.Desktop.Services;

public sealed class CareHubSession : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri(
            (Environment.GetEnvironmentVariable("CAREHUB_GATEWAY_URL") ?? "http://localhost:53615")
            .TrimEnd('/') + "/"),
    };

    private string? _accessToken;

    public bool IsAuthenticated => _accessToken is not null;

    public void Dispose() => _http.Dispose();

    public async Task LoginAsync(string username, string password, CancellationToken ct)
    {
        using var body = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "password",
                ["client_id"] = "carehub-desktop",
                ["client_secret"] = "desktop-secret",
                ["username"] = username.Trim(),
                ["password"] = password,
                ["scope"] = "openid profile offline_access api",
            });
        var res = await _http.PostAsync("connect/token", body, ct);
        res.EnsureSuccessStatusCode();
        await ReadTokenResponseAsync(res, ct);
    }

    public async Task<bool> TrySilentRefreshAsync(CancellationToken ct)
    {
        var rt = RefreshTokenStore.Load();
        if (string.IsNullOrEmpty(rt)) return false;
        using var body = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = rt,
                ["client_id"] = "carehub-desktop",
                ["client_secret"] = "desktop-secret",
                ["scope"] = "openid profile offline_access api",
            });
        var res = await _http.PostAsync("connect/token", body, ct);
        if (!res.IsSuccessStatusCode) return false;
        await ReadTokenResponseAsync(res, ct);
        return true;
    }

    public void Logout()
    {
        _accessToken = null;
        RefreshTokenStore.Clear();
    }

    private async Task ReadTokenResponseAsync(HttpResponseMessage res, CancellationToken ct)
    {
        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var root = doc.RootElement;
        _accessToken = root.GetProperty("access_token").GetString();
        if (root.TryGetProperty("refresh_token", out var rtEl))
        {
            var rt = rtEl.GetString();
            if (!string.IsNullOrEmpty(rt))
                RefreshTokenStore.Save(rt);
        }
    }

    public IReadOnlyList<string> RolesFromToken()
    {
        var roles = new List<string>();
        if (string.IsNullOrEmpty(_accessToken)) return roles;
        var parts = _accessToken.Split('.');
        if (parts.Length < 2) return roles;
        try
        {
            var json = Encoding.UTF8.GetString(ParseBase64Url(parts[1]));
            using var doc = JsonDocument.Parse(json);
            foreach (var name in new[]
                     {
                         "role",
                         "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                     })
            {
                if (!doc.RootElement.TryGetProperty(name, out var el)) continue;
                if (el.ValueKind == JsonValueKind.String)
                    roles.Add(el.GetString()!);
                else if (el.ValueKind == JsonValueKind.Array)
                {
                    foreach (var x in el.EnumerateArray())
                    {
                        if (x.ValueKind == JsonValueKind.String)
                            roles.Add(x.GetString()!);
                    }
                }
            }
        }
        catch
        {
            /* ignore */
        }

        return roles;
    }

    private static byte[] ParseBase64Url(string s)
    {
        var padded = s.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }
        return Convert.FromBase64String(padded);
    }

    public async Task<string> GetJsonAsync(string relativeUri, CancellationToken ct)
    {
        return await SendWithRefreshAsync(
            () => new HttpRequestMessage(HttpMethod.Get, relativeUri),
            ct);
    }

    public async Task<string> PostJsonAsync(string relativeUri, string json, CancellationToken ct)
    {
        return await SendWithRefreshAsync(
            () =>
            {
                var m = new HttpRequestMessage(HttpMethod.Post, relativeUri)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
                return m;
            },
            ct);
    }

    public async Task PostWithoutContentAsync(string relativeUri, CancellationToken ct)
    {
        _ = await SendWithRefreshBytesAsync(
            () => new HttpRequestMessage(HttpMethod.Post, relativeUri),
            ct);
    }

    public async Task SaveBinaryToFileAsync(string relativeUri, string filePath, CancellationToken ct)
    {
        var bytes = await SendWithRefreshBytesAsync(
            () => new HttpRequestMessage(HttpMethod.Get, relativeUri),
            ct);
        await File.WriteAllBytesAsync(filePath, bytes, ct);
    }

    private async Task<string> SendWithRefreshAsync(
        Func<HttpRequestMessage> factory,
        CancellationToken ct)
    {
        var bytes = await SendWithRefreshBytesAsync(factory, ct);
        return Encoding.UTF8.GetString(bytes);
    }

    private async Task<byte[]> SendWithRefreshBytesAsync(
        Func<HttpRequestMessage> factory,
        CancellationToken ct)
    {
        if (_accessToken is null)
            throw new InvalidOperationException("Not signed in.");

        async Task<HttpResponseMessage> OnceAsync()
        {
            using var req = factory();
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
            return await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        }

        var res = await OnceAsync();
        if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if (await TrySilentRefreshAsync(ct))
                res = await OnceAsync();
        }

        res.EnsureSuccessStatusCode();
        return await res.Content.ReadAsByteArrayAsync(ct);
    }

    public T? Deserialize<T>(string json) => JsonSerializer.Deserialize<T>(json, JsonOptions);
}
