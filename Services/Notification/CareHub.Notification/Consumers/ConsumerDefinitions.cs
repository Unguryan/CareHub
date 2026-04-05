using MassTransit;

namespace CareHub.Notification.Consumers;

public class AppointmentCreatedConsumerDefinition : ConsumerDefinition<AppointmentCreatedConsumer>
{
    public AppointmentCreatedConsumerDefinition() => EndpointName = "notification-appointment-created";
}

public class AppointmentCancelledConsumerDefinition : ConsumerDefinition<AppointmentCancelledConsumer>
{
    public AppointmentCancelledConsumerDefinition() => EndpointName = "notification-appointment-cancelled";
}

public class AppointmentRescheduledConsumerDefinition : ConsumerDefinition<AppointmentRescheduledConsumer>
{
    public AppointmentRescheduledConsumerDefinition() => EndpointName = "notification-appointment-rescheduled";
}

public class InvoiceGeneratedConsumerDefinition : ConsumerDefinition<InvoiceGeneratedConsumer>
{
    public InvoiceGeneratedConsumerDefinition() => EndpointName = "notification-invoice-generated";
}

public class LabResultReadyConsumerDefinition : ConsumerDefinition<LabResultReadyConsumer>
{
    public LabResultReadyConsumerDefinition() => EndpointName = "notification-lab-result-ready";
}
