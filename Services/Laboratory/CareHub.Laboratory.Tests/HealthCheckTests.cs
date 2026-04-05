using System.Net;
using CareHub.Laboratory.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Laboratory.Tests;

public class HealthCheckTests : IClassFixture<LaboratoryTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(LaboratoryTestFactory factory)
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
