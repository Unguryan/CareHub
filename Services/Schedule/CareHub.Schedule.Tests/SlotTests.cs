using System.Net;
using System.Net.Http.Json;
using CareHub.Schedule.Models;
using CareHub.Schedule.Tests.Helpers;
using FluentAssertions;
using Xunit;

namespace CareHub.Schedule.Tests;

public class SlotTests : IClassFixture<ScheduleTestFactory>
{
    private readonly HttpClient _client;

    public SlotTests(ScheduleTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<(DoctorResponse Doctor, ShiftResponse Shift)> CreateDoctorWithShiftAsync(
        DateOnly date, TimeOnly start, TimeOnly end, int slotMinutes = 30)
    {
        var docResponse = await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("Slot", "Doctor", "General", ScheduleTestFactory.DefaultBranchId));
        var doctor = (await docResponse.Content.ReadFromJsonAsync<DoctorResponse>())!;

        var shiftResponse = await _client.PostAsJsonAsync($"/api/doctors/{doctor.Id}/shifts",
            new CreateShiftRequest(date, start, end, slotMinutes));
        var shift = (await shiftResponse.Content.ReadFromJsonAsync<ShiftResponse>())!;

        return (doctor, shift);
    }

    [Fact]
    public async Task GetSlots_WithShiftFromNineToNoon_Returns6ThirtyMinuteSlots()
    {
        var date = new DateOnly(2026, 7, 1);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(12, 0), 30);

        var response = await _client.GetAsync($"/api/doctors/{doctor.Id}/slots?date={date:yyyy-MM-dd}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slots = await response.Content.ReadFromJsonAsync<List<SlotResponse>>();
        slots!.Count.Should().Be(6);
        slots[0].SlotTime.Should().Be(new TimeOnly(9, 0));
        slots[1].SlotTime.Should().Be(new TimeOnly(9, 30));
        slots[5].SlotTime.Should().Be(new TimeOnly(11, 30));
    }

    [Fact]
    public async Task GetSlots_WithNoShiftOnDate_ReturnsEmptyList()
    {
        var docResponse = await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("No", "Shift", "General", ScheduleTestFactory.DefaultBranchId));
        var doctor = (await docResponse.Content.ReadFromJsonAsync<DoctorResponse>())!;

        var response = await _client.GetAsync($"/api/doctors/{doctor.Id}/slots?date=2026-08-01");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slots = await response.Content.ReadFromJsonAsync<List<SlotResponse>>();
        slots!.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSlots_ForNonExistentDoctor_Returns404()
    {
        var response = await _client.GetAsync($"/api/doctors/{Guid.NewGuid()}/slots?date=2026-07-01");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSlots_With45MinuteSlots_ReturnsCorrectCount()
    {
        var date = new DateOnly(2026, 7, 2);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(12, 45), 45);

        var response = await _client.GetAsync($"/api/doctors/{doctor.Id}/slots?date={date:yyyy-MM-dd}");

        var slots = await response.Content.ReadFromJsonAsync<List<SlotResponse>>();
        slots!.Count.Should().Be(5);
        slots[0].SlotTime.Should().Be(new TimeOnly(9, 0));
        slots[4].SlotTime.Should().Be(new TimeOnly(12, 0));
    }

    [Fact]
    public async Task ValidateSlot_WithValidAlignedSlot_ReturnsIsValidTrue()
    {
        var date = new DateOnly(2026, 9, 1);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(17, 0), 30);

        var request = new ValidateSlotRequest(doctor.Id, date, new TimeOnly(10, 30));
        var response = await _client.PostAsJsonAsync("/api/slots/validate", request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ValidateSlotResponse>();
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSlot_BeforeShiftStart_ReturnsIsValidFalse()
    {
        var date = new DateOnly(2026, 9, 2);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(17, 0), 30);

        var request = new ValidateSlotRequest(doctor.Id, date, new TimeOnly(8, 0));
        var response = await _client.PostAsJsonAsync("/api/slots/validate", request);

        var result = await response.Content.ReadFromJsonAsync<ValidateSlotResponse>();
        result!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSlot_SlotEndExceedsShiftEnd_ReturnsIsValidFalse()
    {
        var date = new DateOnly(2026, 9, 3);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(12, 0), 30);

        var request = new ValidateSlotRequest(doctor.Id, date, new TimeOnly(11, 45));
        var response = await _client.PostAsJsonAsync("/api/slots/validate", request);

        var result = await response.Content.ReadFromJsonAsync<ValidateSlotResponse>();
        result!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSlot_MisalignedTime_ReturnsIsValidFalse()
    {
        var date = new DateOnly(2026, 9, 4);
        var (doctor, _) = await CreateDoctorWithShiftAsync(date, new TimeOnly(9, 0), new TimeOnly(12, 0), 30);

        var request = new ValidateSlotRequest(doctor.Id, date, new TimeOnly(9, 15));
        var response = await _client.PostAsJsonAsync("/api/slots/validate", request);

        var result = await response.Content.ReadFromJsonAsync<ValidateSlotResponse>();
        result!.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSlot_NoShiftOnDate_ReturnsIsValidFalse()
    {
        var docResponse = await _client.PostAsJsonAsync("/api/doctors",
            new CreateDoctorRequest("No", "Shift2", "General", ScheduleTestFactory.DefaultBranchId));
        var doctor = (await docResponse.Content.ReadFromJsonAsync<DoctorResponse>())!;

        var request = new ValidateSlotRequest(doctor.Id, new DateOnly(2026, 12, 25), new TimeOnly(10, 0));
        var response = await _client.PostAsJsonAsync("/api/slots/validate", request);

        var result = await response.Content.ReadFromJsonAsync<ValidateSlotResponse>();
        result!.IsValid.Should().BeFalse();
    }
}
