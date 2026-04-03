namespace CareHub.Shared.Contracts.Events.Patients;

public record PatientUpdated(
    Guid PatientId,
    Guid UpdatedByUserId,
    DateTime OccurredAt
);
