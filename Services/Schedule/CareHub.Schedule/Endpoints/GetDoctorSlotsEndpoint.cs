using CareHub.Schedule.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Schedule.Endpoints;

public static class GetDoctorSlotsEndpoint
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        [FromQuery] DateOnly date,
        ScheduleService scheduleService)
    {
        var doctor = await scheduleService.GetDoctorByIdAsync(id);
        if (doctor is null)
            return Results.NotFound();

        var slots = await scheduleService.GetAvailableSlotsAsync(id, date);
        return Results.Ok(slots);
    }
}
