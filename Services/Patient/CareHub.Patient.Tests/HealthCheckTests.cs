using System.Net;
using CareHub.Patient.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Patient.Tests;

public class HealthCheckTests : IClassFixture<PatientTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(PatientTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
