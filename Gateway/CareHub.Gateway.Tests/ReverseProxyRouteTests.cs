using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace CareHub.Gateway.Tests;

public class ReverseProxyRouteTests
{
    [Fact]
    public void Appsettings_Defines_Core_Routes_And_Notification_Hub_Path()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "gateway-appsettings.json");
        File.Exists(path).Should().BeTrue("gateway appsettings must be copied to test output");

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var routes = doc.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");

        routes.GetProperty("patient-route").GetProperty("Match").GetProperty("Path").GetString()
            .Should().Be("/api/patients/{**catch-all}");

        var hubPath = routes.GetProperty("notifications-hub-route").GetProperty("Match").GetProperty("Path").GetString();
        hubPath.Should().StartWith("/hubs/notifications");
    }

    [Fact]
    public void Appsettings_Anonymous_Routes_For_Public_Auth_Endpoints()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "gateway-appsettings.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var routes = doc.RootElement.GetProperty("ReverseProxy").GetProperty("Routes");

        routes.GetProperty("identity-auth-route").GetProperty("AuthorizationPolicy").GetString()
            .Should().Be("anonymous");
        routes.GetProperty("identity-connect-route").GetProperty("AuthorizationPolicy").GetString()
            .Should().Be("anonymous");
    }
}
