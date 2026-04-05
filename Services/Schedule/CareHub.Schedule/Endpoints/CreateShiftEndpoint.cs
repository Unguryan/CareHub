using CareHub.Schedule.Exceptions;
using CareHub.Schedule.Models;
using CareHub.Schedule.Services;

namespace CareHub.Schedule.Endpoints;

public static class CreateShiftEndpoint
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        CreateShiftRequest request,
        ScheduleService scheduleService)
    {
        try
        {
            var shift = await scheduleService.CreateShiftAsync(id, request);
            return Results.Created($"/api/shifts/{shift.Id}", shift);
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
