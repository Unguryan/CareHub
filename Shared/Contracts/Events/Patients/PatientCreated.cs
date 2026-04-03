namespace CareHub.Shared.Contracts.Events.Patients;

public record PatientCreated(
    Guid PatientId,
    string FirstName,
    string LastName,
    string PhoneNumber,
    Guid BranchId,
    Guid CreatedByUserId,
    DateTime OccurredAt
);
