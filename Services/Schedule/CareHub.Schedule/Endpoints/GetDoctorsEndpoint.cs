using CareHub.Schedule.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Schedule.Endpoints;

public static class GetDoctorsEndpoint
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        api.MapGet("/doctors", HandleAsync).RequireAuthorization();
        api.MapGet("/doctors/{id:guid}", GetDoctorEndpoint.HandleAsync).RequireAuthorization();
        api.MapPost("/doctors", CreateDoctorEndpoint.HandleAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "Manager"));
        api.MapGet("/doctors/{id:guid}/slots", GetDoctorSlotsEndpoint.HandleAsync).RequireAuthorization();
        api.MapPost("/doctors/{id:guid}/shifts", CreateShiftEndpoint.HandleAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "Manager"));
        api.MapPut("/shifts/{id:guid}", UpdateShiftEndpoint.HandleAsync)
            .RequireAuthorization(p => p.RequireRole("Admin", "Manager"));
        api.MapPost("/slots/validate", ValidateSlotEndpoint.HandleAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        ScheduleService scheduleService,
        [FromQuery] string? specialty,
        [FromQuery] Guid? branchId)
    {
        var doctors = await scheduleService.GetDoctorsAsync(specialty, branchId);
        return Results.Ok(doctors);
    }
}
