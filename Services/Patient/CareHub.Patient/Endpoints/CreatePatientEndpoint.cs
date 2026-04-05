using System.Security.Claims;
using CareHub.Patient.Exceptions;
using CareHub.Patient.Models;
using CareHub.Patient.Services;

namespace CareHub.Patient.Endpoints;

public static class CreatePatientEndpoint
{
    public static async Task<IResult> HandleAsync(
        CreatePatientRequest request,
        HttpContext httpContext,
        PatientService patientService)
    {
        var userId = Guid.Parse(httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var branchId = Guid.Parse(httpContext.User.FindFirstValue("branch_id")
            ?? Guid.Empty.ToString());

        try
        {
            var patient = await patientService.CreateAsync(request, userId, branchId);
            return Results.Created($"/api/patients/{patient.Id}", patient);
        }
        catch (DuplicatePhoneNumberException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
