using CareHub.Reporting.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Patients;
using MassTransit;

namespace CareHub.Reporting.Consumers;

public class ReportingPatientCreatedConsumer : IConsumer<PatientCreated>
{
    private readonly ReportingProjectionService _projection;

    public ReportingPatientCreatedConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<PatientCreated> context) =>
        _projection.ApplyPatientCreatedAsync(context.Message, context.CancellationToken);
}

public class ReportingAppointmentCreatedConsumer : IConsumer<AppointmentCreated>
{
    private readonly ReportingProjectionService _projection;

    public ReportingAppointmentCreatedConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<AppointmentCreated> context) =>
        _projection.ApplyAppointmentCreatedAsync(context.Message, context.CancellationToken);
}

public class ReportingAppointmentCompletedConsumer : IConsumer<AppointmentCompleted>
{
    private readonly ReportingProjectionService _projection;

    public ReportingAppointmentCompletedConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<AppointmentCompleted> context) =>
        _projection.ApplyAppointmentCompletedAsync(context.Message, context.CancellationToken);
}

public class ReportingAppointmentCancelledConsumer : IConsumer<AppointmentCancelled>
{
    private readonly ReportingProjectionService _projection;

    public ReportingAppointmentCancelledConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<AppointmentCancelled> context) =>
        _projection.ApplyAppointmentCancelledAsync(context.Message, context.CancellationToken);
}

public class ReportingPaymentCompletedConsumer : IConsumer<PaymentCompleted>
{
    private readonly ReportingProjectionService _projection;

    public ReportingPaymentCompletedConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<PaymentCompleted> context)
    {
        var key = DedupeKey(context);
        return _projection.ApplyPaymentCompletedAsync(context.Message, key, context.CancellationToken);
    }

    private static string DedupeKey(ConsumeContext<PaymentCompleted> context)
    {
        if (context.MessageId.HasValue)
            return context.MessageId.Value.ToString();
        var m = context.Message;
        return $"pay:{m.InvoiceId:N}:{m.OccurredAt:O}";
    }
}

public class ReportingRefundIssuedConsumer : IConsumer<RefundIssued>
{
    private readonly ReportingProjectionService _projection;

    public ReportingRefundIssuedConsumer(ReportingProjectionService projection) => _projection = projection;

    public Task Consume(ConsumeContext<RefundIssued> context)
    {
        var key = DedupeKey(context);
        return _projection.ApplyRefundIssuedAsync(context.Message, key, context.CancellationToken);
    }

    private static string DedupeKey(ConsumeContext<RefundIssued> context)
    {
        if (context.MessageId.HasValue)
            return context.MessageId.Value.ToString();
        var m = context.Message;
        return $"refund:{m.InvoiceId:N}:{m.OccurredAt:O}";
    }
}

public class ReportingPatientCreatedConsumerDefinition : ConsumerDefinition<ReportingPatientCreatedConsumer>
{
    public ReportingPatientCreatedConsumerDefinition() => EndpointName = "reporting-patient-created";
}

public class ReportingAppointmentCreatedConsumerDefinition : ConsumerDefinition<ReportingAppointmentCreatedConsumer>
{
    public ReportingAppointmentCreatedConsumerDefinition() => EndpointName = "reporting-appointment-created";
}

public class ReportingAppointmentCompletedConsumerDefinition : ConsumerDefinition<ReportingAppointmentCompletedConsumer>
{
    public ReportingAppointmentCompletedConsumerDefinition() => EndpointName = "reporting-appointment-completed";
}

public class ReportingAppointmentCancelledConsumerDefinition : ConsumerDefinition<ReportingAppointmentCancelledConsumer>
{
    public ReportingAppointmentCancelledConsumerDefinition() => EndpointName = "reporting-appointment-cancelled";
}

public class ReportingPaymentCompletedConsumerDefinition : ConsumerDefinition<ReportingPaymentCompletedConsumer>
{
    public ReportingPaymentCompletedConsumerDefinition() => EndpointName = "reporting-payment-completed";
}

public class ReportingRefundIssuedConsumerDefinition : ConsumerDefinition<ReportingRefundIssuedConsumer>
{
    public ReportingRefundIssuedConsumerDefinition() => EndpointName = "reporting-refund-issued";
}
