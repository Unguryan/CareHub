using System.Text.Json;
using CareHub.Shared.Contracts.Events.Identity;
using FluentAssertions;
using Xunit;

namespace CareHub.Shared.Contracts.Tests.Events;

public class IdentityEventsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void UserLoggedIn_RoundTrips_ThroughJson()
    {
        var original = new UserLoggedIn(
            UserId: Guid.NewGuid(),
            PhoneNumber: "+380991234567",
            Roles: ["Doctor", "Auditor"],
            BranchId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<UserLoggedIn>(json, Options);

        result.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void UserLoggedOut_RoundTrips_ThroughJson()
    {
        var original = new UserLoggedOut(
            UserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<UserLoggedOut>(json, Options);

        result.Should().Be(original);
    }
}
