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

public class AppointmentLifecycleTests : IClassFixture<AppointmentTestFactory>
{
    private readonly AppointmentTestFactory _factory;
    private readonly HttpClient _client;

    public AppointmentLifecycleTests(AppointmentTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Reschedule_Publishes_AppointmentRescheduled()
    {
        var created = await CreateAsync(new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc));
        var id = created.Id;

        var put = await _client.PutAsJsonAsync(
            $"/api/appointments/{id}",
            new RescheduleAppointmentRequest(new DateTime(2026, 8, 1, 10, 0, 0, DateTimeKind.Utc), null));
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<AppointmentRescheduled>()).Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_Publishes_AppointmentCancelled()
    {
        var created = await CreateAsync(new DateTime(2026, 8, 2, 9, 0, 0, DateTimeKind.Utc));
        var id = created.Id;

        var post = await _client.PostAsJsonAsync(
            $"/api/appointments/{id}/cancel",
            new CancelAppointmentRequest("Patient requested"));
        post.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<AppointmentCancelled>()).Should().BeTrue();
    }

    [Fact]
    public async Task CheckIn_Then_Complete_Publishes_AppointmentCompleted()
    {
        var created = await CreateAsync(new DateTime(2026, 8, 3, 11, 0, 0, DateTimeKind.Utc));
        var id = created.Id;

        (await _client.PostAsync($"/api/appointments/{id}/checkin", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        var complete = await _client.PostAsJsonAsync(
            $"/api/appointments/{id}/complete",
            new CompleteAppointmentRequest(RequiresLabWork: true));
        complete.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<AppointmentCompleted>()).Should().BeTrue();
    }

    private async Task<AppointmentResponse> CreateAsync(DateTime scheduled)
    {
        var body = new CreateAppointmentRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AppointmentTestFactory.DefaultBranchId,
            scheduled,
            30);
        var response = await _client.PostAsJsonAsync("/api/appointments", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        created.Should().NotBeNull();
        return created!;
    }
}
