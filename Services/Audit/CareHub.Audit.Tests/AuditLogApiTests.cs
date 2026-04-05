using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Audit.Data;
using CareHub.Audit.Endpoints;
using CareHub.Audit.Models;
using CareHub.Audit.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CareHub.Audit.Tests;

public class AuditLogApiTests : IClassFixture<AuditTestFactory>
{
    private readonly AuditTestFactory _factory;
    private readonly HttpClient _client;

    public AuditLogApiTests(AuditTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_WithoutAuth_Returns401()
    {
        using var anon = _factory.CreateClient();
        anon.DefaultRequestHeaders.Add("X-Test-Auth", TestAuthHandler.AnonymousHeader);
        var response = await anon.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_WithInsufficientRole_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Doctor");
        var response = await client.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task List_AsAdmin_Returns200()
    {
        _client.DefaultRequestHeaders.Remove("X-Test-Roles");
        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await _client.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_AsAuditor_Returns200()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Roles", "Auditor");
        var response = await client.GetAsync("/api/audit-logs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task List_FilterByUserId_ReturnsMatchingRows()
    {
        var userId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        await SeedEntriesAsync(userId);

        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await _client.GetAsync($"/api/audit-logs?userId={userId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<AuditLogListResponse>(JsonOptions());
        body!.Items.Should().HaveCount(1);
        body.Items[0].ActorUserId.Should().Be(userId);
    }

    [Fact]
    public async Task List_InvertedFromTo_Returns400()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await _client.GetAsync("/api/audit-logs?from=2026-01-10T00:00:00Z&to=2026-01-01T00:00:00Z");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_ReturnsDetailsJson()
    {
        var id = Guid.Parse("30000000-0000-0000-0000-000000000003");
        await SeedDetailAsync(id);

        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Auditor");
        var response = await _client.GetAsync($"/api/audit-logs/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AuditLogDetailDto>(JsonOptions());
        dto!.DetailsJson.Should().Contain("patient.created");
        dto.Id.Should().Be(id);
    }

    [Fact]
    public async Task GetById_NotFound_Returns404()
    {
        _client.DefaultRequestHeaders.Add("X-Test-Roles", "Admin");
        var response = await _client.GetAsync($"/api/audit-logs/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task SeedEntriesAsync(Guid actorUserId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        db.AuditLogEntries.AddRange(
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                RecordedAt = DateTime.UtcNow,
                ActionType = "patient.created",
                ActorUserId = actorUserId,
                EntityType = "Patient",
                EntityId = Guid.NewGuid(),
                BranchId = AuditTestFactory.DefaultBranchId,
                DetailsJson = "{}"
            },
            new AuditLogEntry
            {
                Id = Guid.NewGuid(),
                RecordedAt = DateTime.UtcNow,
                ActionType = "patient.updated",
                ActorUserId = Guid.NewGuid(),
                EntityType = "Patient",
                EntityId = Guid.NewGuid(),
                BranchId = null,
                DetailsJson = "{}"
            });
        await db.SaveChangesAsync();
    }

    private async Task SeedDetailAsync(Guid id)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuditDbContext>();
        db.AuditLogEntries.Add(new AuditLogEntry
        {
            Id = id,
            RecordedAt = new DateTime(2026, 1, 5, 12, 0, 0, DateTimeKind.Utc),
            ActionType = "patient.created",
            ActorUserId = Guid.NewGuid(),
            EntityType = "Patient",
            EntityId = Guid.NewGuid(),
            BranchId = null,
            DetailsJson = """{"action":"patient.created"}"""
        });
        await db.SaveChangesAsync();
    }

    private static JsonSerializerOptions JsonOptions() =>
        new() { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
