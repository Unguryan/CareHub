using System.Net;
using CareHub.Appointment.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Appointment.Tests;

public class HealthCheckTests : IClassFixture<AppointmentTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(AppointmentTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
