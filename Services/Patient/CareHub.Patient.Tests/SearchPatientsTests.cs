using System.Net;
using System.Net.Http.Json;
using CareHub.Patient.Models;
using CareHub.Patient.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Patient.Tests;

// Seed data (from PatientSeedData):
//   Branch1 (DefaultBranchId): Ivan Petrenko (+380501234567), Olena Kovalenko (+380507654321)
//   Branch2:                   Mykola Shevchenko (+380509876543)
// Test auth handler authenticates as Branch1.

public class SearchPatientsTests : IClassFixture<PatientTestFactory>
{
    private readonly HttpClient _client;

    public SearchPatientsTests(PatientTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Search_Default_ReturnsBranch1PatientsOnly()
    {
        var response = await _client.GetAsync("/patients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        results!.Should().OnlyContain(p => p.BranchId == PatientTestFactory.DefaultBranchId);
        results.Should().HaveCountGreaterOrEqualTo(2); // Ivan + Olena from seed
    }

    [Fact]
    public async Task Search_GlobalTrue_ReturnsAllBranches()
    {
        var response = await _client.GetAsync("/patients?global=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        var branchIds = results!.Select(p => p.BranchId).Distinct();
        branchIds.Should().HaveCountGreaterOrEqualTo(2); // both branches represented
    }

    [Fact]
    public async Task Search_ByLastName_ReturnsMatchingPatient()
    {
        var response = await _client.GetAsync("/patients?q=Kovalenko");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        results!.Should().Contain(p => p.LastName == "Kovalenko");
    }

    [Fact]
    public async Task Search_ByPhone_ReturnsMatchingPatient()
    {
        // +380501234567 is Ivan Petrenko in Branch1
        var response = await _client.GetAsync("/patients?q=%2B380501234567");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        results!.Should().Contain(p => p.FirstName == "Ivan");
    }

    [Fact]
    public async Task Search_Branch2PatientWithoutGlobal_ReturnsEmpty()
    {
        // Mykola is in Branch2, caller is in Branch1 — not visible by default
        var response = await _client.GetAsync("/patients?q=Shevchenko");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        results!.Should().NotContain(p => p.LastName == "Shevchenko");
    }

    [Fact]
    public async Task Search_Branch2PatientWithGlobal_ReturnsPatient()
    {
        var response = await _client.GetAsync("/patients?q=Shevchenko&global=true");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<PatientResponse>>();
        results!.Should().Contain(p => p.LastName == "Shevchenko");
    }
}
