using System.Net;
using CareHub.Billing.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Billing.Tests;

public class HealthCheckTests : IClassFixture<BillingTestFactory>
{
    private readonly HttpClient _client;

    public HealthCheckTests(BillingTestFactory factory)
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
