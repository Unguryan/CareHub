using CareHub.Schedule.Models;
using CareHub.Schedule.Services;

namespace CareHub.Schedule.Endpoints;

public static class CreateDoctorEndpoint
{
    public static async Task<IResult> HandleAsync(
        CreateDoctorRequest request,
        ScheduleService scheduleService)
    {
        var doctor = await scheduleService.CreateDoctorAsync(request);
        return Results.Created($"/api/doctors/{doctor.Id}", doctor);
    }
}
