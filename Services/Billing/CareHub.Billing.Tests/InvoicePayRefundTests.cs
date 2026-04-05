using System.Net;
using System.Net.Http.Json;
using CareHub.Billing.Data;
using CareHub.Billing.Models;
using CareHub.Billing.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Billing;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Billing.Tests;

public class InvoicePayRefundTests : IClassFixture<BillingTestFactory>
{
    private readonly BillingTestFactory _factory;
    private readonly HttpClient _client;

    public InvoicePayRefundTests(BillingTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Pay_Then_Refund_Publishes_Events()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            db.Invoices.Add(new Invoice
            {
                Id = id,
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                BranchId = BillingTestFactory.DefaultBranchId,
                Amount = 500m,
                Currency = "UAH",
                Status = InvoiceStatus.Unpaid,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync();
        }

        var pay = await _client.PostAsync($"/api/invoices/{id}/pay", null);
        pay.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
            (await harness.Published.Any<PaymentCompleted>()).Should().BeTrue();
        }

        var refund = await _client.PostAsJsonAsync(
            $"/api/invoices/{id}/refund",
            new RefundInvoiceRequest("Patient cancelled service"));
        refund.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _factory.Services.CreateScope())
        {
            var harness = scope.ServiceProvider.GetRequiredService<ITestHarness>();
            (await harness.Published.Any<RefundIssued>()).Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            var row = await db.Invoices.SingleAsync(i => i.Id == id);
            row.Status.Should().Be(InvoiceStatus.Refunded);
        }
    }
}
