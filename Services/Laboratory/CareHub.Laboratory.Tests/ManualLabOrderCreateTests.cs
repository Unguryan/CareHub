using System.Net;
using System.Net.Http.Json;
using CareHub.Laboratory.Models;
using CareHub.Laboratory.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Laboratory.Tests;

public class ManualLabOrderCreateTests : IClassFixture<LaboratoryTestFactory>
{
    private readonly HttpClient _client;

    public ManualLabOrderCreateTests(LaboratoryTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Duplicate_AppointmentId_On_Manual_Create_Returns_Conflict()
    {
        var appointmentId = Guid.NewGuid();
        var body = new CreateLabOrderRequest(
            AppointmentId: appointmentId,
            PatientId: Guid.NewGuid(),
            DoctorId: Guid.NewGuid(),
            BranchId: LaboratoryTestFactory.DefaultBranchId);

        var first = await _client.PostAsJsonAsync("/api/lab-orders", body);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await _client.PostAsJsonAsync("/api/lab-orders", body);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
