using System.Net;
using System.Net.Http.Json;
using CareHub.Schedule.Models;
using CareHub.Schedule.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Schedule.Tests;

public class DoctorTests : IClassFixture<ScheduleTestFactory>
{
    private readonly HttpClient _client;

    public DoctorTests(ScheduleTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDoctor_WithValidData_Returns201AndDoctor()
    {
        var request = new CreateDoctorRequest(
            "Olena", "Kovalchuk", "Cardiology", ScheduleTestFactory.DefaultBranchId);

        var response = await _client.PostAsJsonAsync("/api/doctors", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        doctor!.FirstName.Should().Be("Olena");
        doctor.LastName.Should().Be("Kovalchuk");
        doctor.Specialty.Should().Be("Cardiology");
        doctor.BranchId.Should().Be(ScheduleTestFactory.DefaultBranchId);
        doctor.IsActive.Should().BeTrue();
        doctor.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetDoctor_WithExistingId_Returns200()
    {
        var created = await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Ivan", "Petrenko", "Neurology", ScheduleTestFactory.DefaultBranchId));
        var createdDoctor = await created.Content.ReadFromJsonAsync<DoctorResponse>();

        var response = await _client.GetAsync($"/api/doctors/{createdDoctor!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doctor = await response.Content.ReadFromJsonAsync<DoctorResponse>();
        doctor!.Id.Should().Be(createdDoctor.Id);
        doctor.Specialty.Should().Be("Neurology");
    }

    [Fact]
    public async Task GetDoctor_WithNonExistentId_Returns404()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetDoctors_FilterBySpecialty_ReturnsOnlyMatching()
    {
        await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Dmytro", "Bondar", "Oncology", ScheduleTestFactory.DefaultBranchId));
        await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Maria", "Sytnik", "Oncology", ScheduleTestFactory.DefaultBranchId));
        await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Andriy", "Tkach", "Dermatology", ScheduleTestFactory.DefaultBranchId));

        var response = await _client.GetAsync("/api/doctors?specialty=Oncology");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doctors = (await response.Content.ReadFromJsonAsync<List<DoctorResponse>>())!;
        doctors.Should().OnlyContain(d => d.Specialty == "Oncology");
        doctors.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task GetDoctors_FilterByBranch_ReturnsOnlyMatching()
    {
        var branch2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
        await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Natalia", "Lysenko", "Pediatrics", ScheduleTestFactory.DefaultBranchId));
        await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Vasyl", "Moroz", "Pediatrics", branch2));

        var response = await _client.GetAsync($"/api/doctors?branchId={branch2}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var doctors = (await response.Content.ReadFromJsonAsync<List<DoctorResponse>>())!;
        doctors.Should().OnlyContain(d => d.BranchId == branch2);
    }
}
