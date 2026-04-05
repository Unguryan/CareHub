using CareHub.Document.Consumers;
using CareHub.Document.Data;
using CareHub.Document.Models;
using CareHub.Document.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Laboratory;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Document.Tests;

public class DocumentConsumerTests : IClassFixture<DocumentTestFactory>
{
    private readonly DocumentTestFactory _factory;

    public DocumentConsumerTests(DocumentTestFactory factory)
    {
        _factory = factory;
        _ = _factory.Server;
    }

    [Fact]
    public async Task InvoiceGenerated_Persists_Invoice_Document()
    {
        var invoiceId = Guid.NewGuid();
        var msg = new InvoiceGenerated(
            invoiceId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            DocumentTestFactory.DefaultBranchId,
            125.50m,
            "UAH",
            new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc));

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(msg);
            var consumed = await harness.GetConsumerHarness<InvoiceGeneratedConsumer>().Consumed.Any();
            consumed.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var row = await db.StoredDocuments.SingleOrDefaultAsync(
                d => d.EntityId == invoiceId && d.Kind == DocumentKind.Invoice);
            row.Should().NotBeNull();
            row!.EntityType.Should().Be("Invoice");
            row.Source.Should().Be(DocumentSource.Event);
        }
        finally
        {
            await harness.Stop();
        }
    }

    [Fact]
    public async Task LabResultReady_Persists_Lab_Document_With_Fallback_When_Internal_Disabled()
    {
        var labOrderId = Guid.NewGuid();
        var msg = new LabResultReady(
            labOrderId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow);

        using var scope = _factory.Services.CreateScope();
        var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
        await harness.Start();
        try
        {
            await harness.Bus.Publish(msg);
            var consumed = await harness.GetConsumerHarness<LabResultReadyConsumer>().Consumed.Any();
            consumed.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<DocumentDbContext>();
            var row = await db.StoredDocuments.SingleOrDefaultAsync(
                d => d.EntityId == labOrderId && d.Kind == DocumentKind.LabResult);
            row.Should().NotBeNull();
            row!.EntityType.Should().Be("LabOrder");
        }
        finally
        {
            await harness.Stop();
        }
    }
}
