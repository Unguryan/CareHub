using System.Net;
using System.Net.Http.Headers;

namespace CareHub.Appointment.Services;

public class PatientClient
{
    private readonly HttpClient _http;

    public PatientClient(HttpClient http) => _http = http;

    public async Task EnsurePatientExistsAsync(Guid patientId, string? bearerToken, CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/patients/{patientId}");
        if (!string.IsNullOrEmpty(bearerToken))
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerToken);

        var response = await _http.SendAsync(req, ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            throw new KeyNotFoundException($"Patient {patientId} not found.");
        response.EnsureSuccessStatusCode();
    }
}
