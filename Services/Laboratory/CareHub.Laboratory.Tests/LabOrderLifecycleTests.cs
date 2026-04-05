using System.Net;
using System.Net.Http.Json;
using CareHub.Laboratory.Data;
using CareHub.Laboratory.Models;
using CareHub.Laboratory.Tests.Helpers;
using CareHub.Shared.Contracts.Events.Laboratory;
using FluentAssertions;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Laboratory.Tests;

public class LabOrderLifecycleTests : IClassFixture<LaboratoryTestFactory>
{
    private readonly LaboratoryTestFactory _factory;
    private readonly HttpClient _client;

    public LabOrderLifecycleTests(LaboratoryTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReceiveSample_Then_EnterResult_Publishes_LabResultReady()
    {
        var id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LaboratoryDbContext>();
            db.LabOrders.Add(new LabOrder
            {
                Id = id,
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                BranchId = LaboratoryTestFactory.DefaultBranchId,
                Status = LabOrderStatus.Pending,
                CreatedAt = now,
                UpdatedAt = now,
            });
            await db.SaveChangesAsync();
        }

        var receive = await _client.PostAsync($"/api/lab-orders/{id}/receive-sample", null);
        receive.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await _client.PostAsJsonAsync(
            $"/api/lab-orders/{id}/result",
            new EnterLabResultRequest("Hemoglobin within reference range."));
        result.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope2 = _factory.Services.CreateScope();
        var harness = scope2.ServiceProvider.GetRequiredService<ITestHarness>();
        (await harness.Published.Any<LabResultReady>()).Should().BeTrue();

        var published = harness.Published.Select<LabResultReady>()
            .Select(x => x.Context.Message)
            .First(m => m.LabOrderId == id);
        published.PatientId.Should().NotBeEmpty();
        published.DoctorId.Should().NotBeEmpty();
        published.LabTechnicianId.Should().Be(LaboratoryTestFactory.DefaultUserId);

        var db2 = scope2.ServiceProvider.GetRequiredService<LaboratoryDbContext>();
        var row = await db2.LabOrders.SingleAsync(o => o.Id == id);
        row.Status.Should().Be(LabOrderStatus.Completed);
        row.ResultSummary.Should().Contain("Hemoglobin");
    }
}
