using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CareHub.Patient.Models;
using CareHub.Patient.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Patient.Tests;

public class PatientCrudTests : IClassFixture<PatientTestFactory>
{
    private readonly HttpClient _client;

    public PatientCrudTests(PatientTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatePatient_WithValidData_Returns201AndPatient()
    {
        var request = new CreatePatientRequest(
            "Anna", "Sydorenko", "+380991110001", null, new DateOnly(1995, 6, 10));

        var response = await _client.PostAsJsonAsync("/api/patients", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient!.FirstName.Should().Be("Anna");
        patient.LastName.Should().Be("Sydorenko");
        patient.PhoneNumber.Should().Be("+380991110001");
        patient.BranchId.Should().Be(PatientTestFactory.DefaultBranchId);
    }

    [Fact]
    public async Task CreatePatient_WithDuplicatePhone_Returns409()
    {
        // +380501234567 is seeded by PatientSeedData (Ivan Petrenko, Branch1)
        var request = new CreatePatientRequest(
            "Other", "Person", "+380501234567", null, new DateOnly(2000, 1, 1));

        var response = await _client.PostAsJsonAsync("/api/patients", request);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetPatient_WithExistingId_Returns200()
    {
        // Create a patient first
        var created = await _client.PostAsJsonAsync("/api/patients",
            new CreatePatientRequest("Dmytro", "Bondarenko", "+380991110002", "d@test.com", new DateOnly(1988, 4, 20)));
        var createdPatient = await created.Content.ReadFromJsonAsync<PatientResponse>();

        // Get it
        var response = await _client.GetAsync($"/api/patients/{createdPatient!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patient = await response.Content.ReadFromJsonAsync<PatientResponse>();
        patient!.Id.Should().Be(createdPatient.Id);
        patient.Email.Should().Be("d@test.com");
    }

    [Fact]
    public async Task GetPatient_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/patients/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePatient_WithValidData_Returns200AndUpdated()
    {
        // Create
        var created = await _client.PostAsJsonAsync("/api/patients",
            new CreatePatientRequest("Vasyl", "Kravchenko", "+380991110003", null, new DateOnly(1975, 9, 3)));
        var createdPatient = await created.Content.ReadFromJsonAsync<PatientResponse>();

        // Update
        var updateRequest = new UpdatePatientRequest(
            "Vasyl", "Kravchenko-Updated", "+380991110003", "v@test.com", new DateOnly(1975, 9, 3));
        var response = await _client.PutAsJsonAsync($"/api/patients/{createdPatient!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PatientResponse>();
        updated!.LastName.Should().Be("Kravchenko-Updated");
        updated.Email.Should().Be("v@test.com");
    }

    [Fact]
    public async Task GetPatientHistory_ReturnsEmptyList()
    {
        // Create a patient
        var created = await _client.PostAsJsonAsync("/api/patients",
            new CreatePatientRequest("Test", "History", "+380991110004", null, new DateOnly(1990, 1, 1)));
        var patient = await created.Content.ReadFromJsonAsync<PatientResponse>();

        var response = await _client.GetAsync($"/api/patients/{patient!.Id}/history");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var history = await response.Content.ReadFromJsonAsync<JsonElement>();
        history.GetArrayLength().Should().Be(0);
    }
}
