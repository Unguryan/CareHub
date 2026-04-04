using System.Security.Claims;
using CareHub.Patient.Exceptions;
using CareHub.Patient.Models;
using CareHub.Patient.Services;

namespace CareHub.Patient.Endpoints;

public static class UpdatePatientEndpoint
{
    public static async Task<IResult> HandleAsync(
        Guid id,
        UpdatePatientRequest request,
        HttpContext httpContext,
        PatientService patientService)
    {
        var userId = Guid.Parse(httpContext.User.FindFirstValue("sub")
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var patient = await patientService.UpdateAsync(id, request, userId);
            return Results.Ok(patient);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (DuplicatePhoneNumberException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
