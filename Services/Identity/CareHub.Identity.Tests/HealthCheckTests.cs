using System.Net;
using FluentAssertions;
using CareHub.Identity.Tests.Helpers;
using Xunit;

namespace CareHub.Identity.Tests;

public class HealthCheckTests : IClassFixture<IdentityTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(IdentityTestFactory factory)
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
