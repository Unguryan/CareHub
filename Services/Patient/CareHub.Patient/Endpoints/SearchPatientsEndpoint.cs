using System.Security.Claims;
using CareHub.Patient.Endpoints;
using CareHub.Patient.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Patient.Endpoints;

public static class SearchPatientsEndpoint
{
    public static IEndpointRouteBuilder MapPatientEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");
        api.MapGet("/patients", HandleAsync).RequireAuthorization();
        api.MapGet("/patients/{id:guid}", GetPatientEndpoint.HandleAsync).RequireAuthorization();
        api.MapPost("/patients", CreatePatientEndpoint.HandleAsync)
            .RequireAuthorization(p => p.RequireRole("Receptionist", "Admin"));
        api.MapPut("/patients/{id:guid}", UpdatePatientEndpoint.HandleAsync)
            .RequireAuthorization(p => p.RequireRole("Receptionist", "Admin"));
        api.MapGet("/patients/{id:guid}/history", GetPatientHistoryEndpoint.HandleAsync).RequireAuthorization();
        return app;
    }

    private static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        PatientService patientService,
        [FromQuery] string? q,
        [FromQuery] Guid? branchId,
        [FromQuery] bool global = false)
    {
        var callerBranchId = Guid.Parse(
            httpContext.User.FindFirstValue("branch_id")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? Guid.Empty.ToString());

        var results = await patientService.SearchAsync(q, branchId, global, callerBranchId);
        return Results.Ok(results);
    }
}
