using System.Text.Json;
using CareHub.Shared.Contracts.Events.Laboratory;
using FluentAssertions;
using Xunit;

namespace CareHub.Shared.Contracts.Tests.Events;

public class LaboratoryEventsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void LabResultReady_RoundTrips_ThroughJson()
    {
        var original = new LabResultReady(
            LabOrderId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            LabTechnicianId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<LabResultReady>(json, Options);

        result.Should().Be(original);
    }
}
