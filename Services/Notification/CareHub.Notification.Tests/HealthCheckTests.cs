using CareHub.Notification.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Notification.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task HealthCheck_Returns200()
    {
        using var factory = new NotificationTestFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }
}
