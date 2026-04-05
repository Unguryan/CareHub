using System.Net;
using CareHub.Schedule.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Schedule.Tests;

public class HealthCheckTests : IClassFixture<ScheduleTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(ScheduleTestFactory factory)
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
