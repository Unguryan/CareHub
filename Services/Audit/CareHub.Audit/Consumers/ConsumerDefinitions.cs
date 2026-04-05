using MassTransit;

namespace CareHub.Audit.Consumers;

public class UserLoggedInConsumerDefinition : ConsumerDefinition<UserLoggedInConsumer>
{
    public UserLoggedInConsumerDefinition() => EndpointName = "audit-user-logged-in";
}

public class UserLoggedOutConsumerDefinition : ConsumerDefinition<UserLoggedOutConsumer>
{
    public UserLoggedOutConsumerDefinition() => EndpointName = "audit-user-logged-out";
}

public class PatientCreatedConsumerDefinition : ConsumerDefinition<PatientCreatedConsumer>
{
    public PatientCreatedConsumerDefinition() => EndpointName = "audit-patient-created";
}

public class PatientUpdatedConsumerDefinition : ConsumerDefinition<PatientUpdatedConsumer>
{
    public PatientUpdatedConsumerDefinition() => EndpointName = "audit-patient-updated";
}

public class AppointmentCreatedConsumerDefinition : ConsumerDefinition<AppointmentCreatedConsumer>
{
    public AppointmentCreatedConsumerDefinition() => EndpointName = "audit-appointment-created";
}

public class AppointmentCancelledConsumerDefinition : ConsumerDefinition<AppointmentCancelledConsumer>
{
    public AppointmentCancelledConsumerDefinition() => EndpointName = "audit-appointment-cancelled";
}

public class AppointmentRescheduledConsumerDefinition : ConsumerDefinition<AppointmentRescheduledConsumer>
{
    public AppointmentRescheduledConsumerDefinition() => EndpointName = "audit-appointment-rescheduled";
}

public class AppointmentCompletedConsumerDefinition : ConsumerDefinition<AppointmentCompletedConsumer>
{
    public AppointmentCompletedConsumerDefinition() => EndpointName = "audit-appointment-completed";
}

public class InvoiceGeneratedConsumerDefinition : ConsumerDefinition<InvoiceGeneratedConsumer>
{
    public InvoiceGeneratedConsumerDefinition() => EndpointName = "audit-invoice-generated";
}

public class PaymentCompletedConsumerDefinition : ConsumerDefinition<PaymentCompletedConsumer>
{
    public PaymentCompletedConsumerDefinition() => EndpointName = "audit-payment-completed";
}

public class RefundIssuedConsumerDefinition : ConsumerDefinition<RefundIssuedConsumer>
{
    public RefundIssuedConsumerDefinition() => EndpointName = "audit-refund-issued";
}
