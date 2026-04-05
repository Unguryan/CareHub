using CareHub.Schedule.Services;

namespace CareHub.Schedule.Endpoints;

public static class GetDoctorEndpoint
{
    public static async Task<IResult> HandleAsync(Guid id, ScheduleService scheduleService)
    {
        var doctor = await scheduleService.GetDoctorByIdAsync(id);
        return doctor is null ? Results.NotFound() : Results.Ok(doctor);
    }
}
