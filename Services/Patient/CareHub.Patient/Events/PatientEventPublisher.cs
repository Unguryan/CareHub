using CareHub.Shared.Contracts.Events.Patients;
using MassTransit;
using PatientEntity = CareHub.Patient.Models.Patient;

namespace CareHub.Patient.Events;

public class PatientEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public PatientEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishPatientCreatedAsync(PatientEntity patient, Guid createdByUserId)
        => _publishEndpoint.Publish(new PatientCreated(
            PatientId: patient.Id,
            FirstName: patient.FirstName,
            LastName: patient.LastName,
            PhoneNumber: patient.PhoneNumber,
            BranchId: patient.BranchId,
            CreatedByUserId: createdByUserId,
            OccurredAt: DateTime.UtcNow));

    public Task PublishPatientUpdatedAsync(PatientEntity patient, Guid updatedByUserId)
        => _publishEndpoint.Publish(new PatientUpdated(
            PatientId: patient.Id,
            UpdatedByUserId: updatedByUserId,
            OccurredAt: DateTime.UtcNow));
}
