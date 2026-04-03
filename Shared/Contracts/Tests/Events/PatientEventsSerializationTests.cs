using System.Text.Json;
using CareHub.Shared.Contracts.Events.Patients;
using FluentAssertions;
using Xunit;

namespace CareHub.Shared.Contracts.Tests.Events;

public class PatientEventsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void PatientCreated_RoundTrips_ThroughJson()
    {
        var original = new PatientCreated(
            PatientId: Guid.NewGuid(),
            FirstName: "Ivan",
            LastName: "Petrenko",
            PhoneNumber: "+380991234567",
            BranchId: Guid.NewGuid(),
            CreatedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<PatientCreated>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void PatientUpdated_RoundTrips_ThroughJson()
    {
        var original = new PatientUpdated(
            PatientId: Guid.NewGuid(),
            UpdatedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<PatientUpdated>(json, Options);

        result.Should().Be(original);
    }
}
