using System.Text.Json;
using CareHub.Shared.Contracts.Events.Billing;
using FluentAssertions;
using Xunit;

namespace CareHub.Shared.Contracts.Tests.Events;

public class BillingEventsSerializationTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    [Fact]
    public void InvoiceGenerated_RoundTrips_ThroughJson()
    {
        var original = new InvoiceGenerated(
            InvoiceId: Guid.NewGuid(),
            AppointmentId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            Amount: 150.00m,
            Currency: "UAH",
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<InvoiceGenerated>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void PaymentCompleted_RoundTrips_ThroughJson()
    {
        var original = new PaymentCompleted(
            InvoiceId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            Amount: 150.00m,
            Currency: "UAH",
            ProcessedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<PaymentCompleted>(json, Options);

        result.Should().Be(original);
    }

    [Fact]
    public void RefundIssued_RoundTrips_ThroughJson()
    {
        var original = new RefundIssued(
            InvoiceId: Guid.NewGuid(),
            PatientId: Guid.NewGuid(),
            BranchId: Guid.NewGuid(),
            Amount: 50.00m,
            Currency: "UAH",
            Reason: "Service not rendered",
            IssuedByUserId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow
        );

        var json = JsonSerializer.Serialize(original, Options);
        var result = JsonSerializer.Deserialize<RefundIssued>(json, Options);

        result.Should().Be(original);
    }
}
