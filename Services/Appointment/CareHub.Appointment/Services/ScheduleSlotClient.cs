using System.Net.Http.Headers;
using System.Net.Http.Json;
using CareHub.Appointment.Exceptions;
using CareHub.Appointment.Models;

namespace CareHub.Appointment.Services;

public class ScheduleSlotClient
{
    private readonly HttpClient _http;

    public ScheduleSlotClient(HttpClient http) => _http = http;

    public async Task EnsureSlotIsValidAsync(
        Guid doctorId,
        DateTime scheduledAtUtc,
        int durationMinutes,
        string? bearerToken,
        CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(scheduledAtUtc);
        var time = TimeOnly.FromDateTime(scheduledAtUtc);
        var body = new ScheduleValidateSlotRequest(doctorId, date, time);

        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/slots/validate")
        {
            Content = JsonContent.Create(body)
        };
        if (!string.IsNullOrEmpty(bearerToken))
            req.Headers.Authorization = AuthenticationHeaderValue.Parse(bearerToken);

        var response = await _http.SendAsync(req, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ScheduleValidateSlotResponse>(cancellationToken: ct);
        if (result is null || !result.IsValid)
            throw new SlotValidationFailedException(result?.Reason ?? "Slot validation failed.");
    }
}
