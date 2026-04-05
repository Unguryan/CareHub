using System.Net;
using System.Net.Http.Json;
using CareHub.Appointment.Models;
using CareHub.Appointment.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Appointment.Tests;

public class AppointmentOverlapTests : IClassFixture<AppointmentTestFactory>
{
    private readonly HttpClient _client;

    public AppointmentOverlapTests(AppointmentTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Two_Overlapping_Bookings_Same_Doctor_Returns409_On_Second()
    {
        var doctorId = Guid.NewGuid();
        var patientA = Guid.NewGuid();
        var patientB = Guid.NewGuid();
        var scheduled = new DateTime(2026, 7, 1, 14, 0, 0, DateTimeKind.Utc);

        var first = new CreateAppointmentRequest(
            patientA,
            doctorId,
            AppointmentTestFactory.DefaultBranchId,
            scheduled,
            30);
        var second = new CreateAppointmentRequest(
            patientB,
            doctorId,
            AppointmentTestFactory.DefaultBranchId,
            scheduled.AddMinutes(15),
            30);

        (await _client.PostAsJsonAsync("/api/appointments", first)).StatusCode.Should().Be(HttpStatusCode.Created);
        (await _client.PostAsJsonAsync("/api/appointments", second)).StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
