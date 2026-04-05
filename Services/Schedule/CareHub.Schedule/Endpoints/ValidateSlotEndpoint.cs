using CareHub.Schedule.Models;
using CareHub.Schedule.Services;

namespace CareHub.Schedule.Endpoints;

public static class ValidateSlotEndpoint
{
    public static async Task<IResult> HandleAsync(
        ValidateSlotRequest request,
        ScheduleService scheduleService)
    {
        var result = await scheduleService.ValidateSlotAsync(request);
        return Results.Ok(result);
    }
}
