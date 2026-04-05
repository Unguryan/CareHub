using System.Net;
using CareHub.Audit.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Audit.Tests;

public class HealthCheckTests : IClassFixture<AuditTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(AuditTestFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task HealthCheck_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
