using CareHub.Patient.Services;

namespace CareHub.Patient.Endpoints;

public static class GetPatientEndpoint
{
    public static async Task<IResult> HandleAsync(Guid id, PatientService patientService)
    {
        var patient = await patientService.GetByIdAsync(id);
        return patient is null ? Results.NotFound() : Results.Ok(patient);
    }
}
