using System.Net;
using System.Net.Http.Json;
using CareHub.Appointment.Models;
using CareHub.Appointment.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Appointments;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Appointment.Tests;

public class AppointmentCreateTests : IClassFixture<AppointmentTestFactory>
{
    private readonly AppointmentTestFactory _factory;
    private readonly HttpClient _client;

    public AppointmentCreateTests(AppointmentTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Post_Appointments_Returns201_And_Publishes_AppointmentCreated()
    {
        var scheduled = new DateTime(2026, 6, 15, 10, 0, 0, DateTimeKind.Utc);
        var body = new CreateAppointmentRequest(
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            BranchId: AppointmentTestFactory.DefaultBranchId,
            ScheduledAt: scheduled,
            DurationMinutes: 30);

        var response = await _client.PostAsJsonAsync("/api/appointments", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<AppointmentCreated>()).Should().BeTrue();
    }
}
