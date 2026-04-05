using CareHub.Laboratory.Models;
using CareHub.Shared.Contracts.Events.Laboratory;
using MassTransit;

namespace CareHub.Laboratory.Events;

public class LaboratoryEventPublisher
{
    private readonly IPublishEndpoint _publish;

    public LaboratoryEventPublisher(IPublishEndpoint publish) => _publish = publish;

    public Task PublishLabResultReadyAsync(LabOrder order, Guid labTechnicianId, DateTime occurredAt)
        => _publish.Publish(new LabResultReady(
            LabOrderId: order.Id,
            AppointmentId: order.AppointmentId,
            PatientId: order.PatientId,
            DoctorId: order.DoctorId,
            LabTechnicianId: labTechnicianId,
            OccurredAt: occurredAt));

}
