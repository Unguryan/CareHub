using System.Text.Json;
using CareHub.Shared.Contracts.Events.Appointments;
using FluentAssertions;
using Xunit;

namespace CareHub.Shared.Contracts.Tests.Events;

public class AppointmentEventsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AppointmentCreated_RoundTrips_ThroughJson()
    {
        var original = new AppointmentCreated(
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            ScheduledAt: new DateTime(2026, 4, 10, 9, 0, 0, DateTimeKind.Utc),
            CreatedByUserId: Guid.NewGuid(),
            OccurredAt: new DateTime(2026, 4, 3, 8, 0, 0, DateTimeKind.Utc)
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<AppointmentCreated>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void AppointmentCancelled_RoundTrips_ThroughJson()
    {
        var original = new AppointmentCancelled(
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            Reason: "Patient request",
            CancelledByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<AppointmentCancelled>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void AppointmentRescheduled_RoundTrips_ThroughJson()
    {
        var original = new AppointmentRescheduled(
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            PreviousScheduledAt: new DateTime(2026, 4, 5, 10, 0, 0, DateTimeKind.Utc),
            NewScheduledAt: new DateTime(2026, 4, 7, 14, 0, 0, DateTimeKind.Utc),
            RescheduledByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<AppointmentRescheduled>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void AppointmentCompleted_RoundTrips_ThroughJson()
    {
        var original = new AppointmentCompleted(
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            RequiresLabWork: true,
            CompletedAt: DateTime.UtcNow,
            CompletedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<AppointmentCompleted>(json, Options);

        result.Should().Be(original);
    }
}
