using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Reporting.Data;
using CareHub.Reporting.Models;
using CareHub.Reporting.Models.Reports.V1;
using CareHub.Reporting.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Reporting.Tests;

[Collection("Reporting")]
public class ReportApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string Range =
        "from=2026-04-01T00:00:00Z&to=2026-04-30T23:59:59Z";

    private readonly ReportingTestFactory _factory;

    public ReportApiTests(ReportingTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Visits_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", TestAuthHandler.AnonymousHeader);
        var r = await client.GetAsync($"/api/reports/visits?{Range}");
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Visits_WithInsufficientRole_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Doctor");
        var r = await client.GetAsync($"/api/reports/visits?{Range}");
        r.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Visits_AsAdmin_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var r = await client.GetAsync($"/api/reports/visits?{Range}");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visits_AsAuditor_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Auditor");
        var r = await client.GetAsync($"/api/reports/visits?{Range}");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Visits_Manager_Global_Forbidden()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Manager");
        var r = await client.GetAsync($"/api/reports/visits?{Range}&global=true");
        r.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Visits_MissingRange_Returns400()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var r = await client.GetAsync("/api/reports/visits");
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Visits_ReturnsSeededAggregates()
    {
        var doctorId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var branchId = ReportingTestFactory.DefaultBranchId;
        var scheduled = new DateTime(2026, 4, 10, 10, 0, 0, DateTimeKind.Utc);
        var completed = new DateTime(2026, 4, 12, 11, 0, 0, DateTimeKind.Utc);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ReportingDbContext>();
            db.ReportAppointmentFacts.Add(new ReportAppointmentFact
            {
                AppointmentId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                DoctorId = doctorId,
                BranchId = branchId,
                ScheduledAt = scheduled,
                CompletedAt = completed
            });
            await db.SaveChangesAsync();
        }

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var r = await client.GetAsync($"/api/reports/visits?{Range}");
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await r.Content.ReadFromJsonAsync<VisitsReportResponse>(JsonOptions);
        body.Should().NotBeNull();
        body!.Rows.Should().NotBeEmpty();
        var doctorRows = body.Rows.Where(x => x.DoctorId == doctorId).ToList();
        doctorRows.Sum(x => x.ScheduledCount).Should().BeGreaterThan(0);
        doctorRows.Sum(x => x.CompletedCount).Should().BeGreaterThan(0);
    }
}
