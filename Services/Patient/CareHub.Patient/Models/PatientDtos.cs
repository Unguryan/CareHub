namespace CareHub.Patient.Models;

public record CreatePatientRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    DateOnly DateOfBirth);

public record UpdatePatientRequest(
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    DateOnly DateOfBirth);

public record PatientResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string? Email,
    DateOnly DateOfBirth,
    Guid BranchId,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static PatientResponse FromEntity(Patient p) => new(
        p.Id, p.FirstName, p.LastName, p.PhoneNumber, p.Email,
        p.DateOfBirth, p.BranchId, p.CreatedAt, p.UpdatedAt);
}
