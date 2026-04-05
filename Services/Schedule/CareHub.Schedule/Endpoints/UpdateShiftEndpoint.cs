using CareHub.Schedule.Exceptions;
using CareHub.Schedule.Models;
using CareHub.Schedule.Services;

namespace CareHub.Schedule.Endpoints;

public static class UpdateShiftEndpoint
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        UpdateShiftRequest request,
        ScheduleService scheduleService)
    {
        try
        {
            var shift = await scheduleService.UpdateShiftAsync(id, request);
            return Results.Ok(shift);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidShiftException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }
}
