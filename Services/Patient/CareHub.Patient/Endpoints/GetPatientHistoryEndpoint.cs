using CareHub.Patient.Services;

namespace CareHub.Patient.Endpoints;

public static class GetPatientHistoryEndpoint
{
    public static async Task<IResult> HandleAsync(Guid id, PatientService patientService)
    {
        var history = await patientService.GetHistoryAsync(id);
        return Results.Ok(history);
    }
}
