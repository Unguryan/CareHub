using System.Text.Json;
using Microsoft.Extensions.Options;
using CareHub.Document.Options;

namespace CareHub.Document.Clients;

public sealed class HttpLaboratoryInternalClient : ILaboratoryInternalClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _http;
    private readonly IOptionsMonitor<LaboratoryInternalOptions> _options;

    public HttpLaboratoryInternalClient(HttpClient http, IOptionsMonitor<LaboratoryInternalOptions> options)
    {
        _http = http;
        _options = options;
    }

    public async Task<LaboratoryDocumentContextDto?> GetLabOrderDocumentContextAsync(
        Guid labOrderId,
        CancellationToken cancellationToken = default)
    {
        var baseUrl = _options.CurrentValue.BaseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return null;

        var key = _options.CurrentValue.ApiKey;
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/internal/lab-orders/{labOrderId:D}/document-context");
        if (!string.IsNullOrEmpty(key))
            req.Headers.TryAddWithoutValidation("X-CareHub-Document-Key", key);

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<LaboratoryDocumentContextDto>(stream, JsonOptions, cancellationToken);
    }
}
