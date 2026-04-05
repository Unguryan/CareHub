using System.Net;
using System.Net.Http.Json;
using CareHub.Schedule.Models;
using CareHub.Schedule.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Schedule.Tests;

public class ShiftTests : IClassFixture<ScheduleTestFactory>
{
    private readonly HttpClient _client;

    public ShiftTests(ScheduleTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<DoctorResponse> CreateDoctorAsync(string lastName = "Kovalenko")
    {
        var response = await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Test", lastName, "General", ScheduleTestFactory.DefaultBranchId));
        return (await response.Content.ReadFromJsonAsync<DoctorResponse>())!;
    }

    [Fact]
    public async Task CreateShift_WithValidData_Returns201AndShift()
    {
        var doctor = await CreateDoctorAsync("ShiftTest1");
        var request = new CreateShiftRequest(
            new DateOnly(2026, 5, 1),
            new TimeOnly(9, 0),
            new TimeOnly(17, 0),
            30,
            "Room 101");

        var response = await _client.PostAsJsonAsync($"/api/doctors/{doctor.Id}/shifts", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var shift = await response.Content.ReadFromJsonAsync<ShiftResponse>();
        shift!.DoctorId.Should().Be(doctor.Id);
        shift.Date.Should().Be(new DateOnly(2026, 5, 1));
        shift.StartTime.Should().Be(new TimeOnly(9, 0));
        shift.EndTime.Should().Be(new TimeOnly(17, 0));
        shift.SlotDurationMinutes.Should().Be(30);
        shift.RoomNumber.Should().Be("Room 101");
    }

    [Fact]
    public async Task CreateShift_ForNonExistentDoctor_Returns404()
    {
        var request = new CreateShiftRequest(
            new DateOnly(2026, 5, 1),
            new TimeOnly(9, 0),
            new TimeOnly(12, 0));

        var response = await _client.PostAsJsonAsync($"/api/doctors/{Guid.NewGuid()}/shifts", request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateShift_WithStartTimeAfterEndTime_Returns400()
    {
        var doctor = await CreateDoctorAsync("ShiftTest2");
        var request = new CreateShiftRequest(
            new DateOnly(2026, 5, 2),
            new TimeOnly(17, 0),
            new TimeOnly(9, 0));

        var response = await _client.PostAsJsonAsync($"/api/doctors/{doctor.Id}/shifts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateShift_WithZeroSlotDuration_Returns400()
    {
        var doctor = await CreateDoctorAsync("ShiftTest3");
        var request = new CreateShiftRequest(
            new DateOnly(2026, 5, 3),
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            0);

        var response = await _client.PostAsJsonAsync($"/api/doctors/{doctor.Id}/shifts", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateShift_WithValidData_Returns200AndUpdated()
    {
        var doctor = await CreateDoctorAsync("ShiftTest4");
        var created = await _client.PostAsJsonAsync($"/api/doctors/{doctor.Id}/shifts",
            new CreateShiftRequest(new DateOnly(2026, 6, 1), new TimeOnly(8, 0), new TimeOnly(14, 0), 30, "Room A"));
        var shift = (await created.Content.ReadFromJsonAsync<ShiftResponse>())!;

        var update = new UpdateShiftRequest(
            new DateOnly(2026, 6, 1),
            new TimeOnly(9, 0),
            new TimeOnly(15, 0),
            45,
            "Room B");

        var response = await _client.PutAsJsonAsync($"/api/shifts/{shift.Id}", update);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<ShiftResponse>();
        updated!.StartTime.Should().Be(new TimeOnly(9, 0));
        updated.EndTime.Should().Be(new TimeOnly(15, 0));
        updated.SlotDurationMinutes.Should().Be(45);
        updated.RoomNumber.Should().Be("Room B");
    }

    [Fact]
    public async Task UpdateShift_WithNonExistentId_Returns404()
    {
        var update = new UpdateShiftRequest(
            new DateOnly(2026, 6, 1),
            new TimeOnly(9, 0),
            new TimeOnly(12, 0),
            30,
            null);

        var response = await _client.PutAsJsonAsync($"/api/shifts/{Guid.NewGuid()}", update);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
