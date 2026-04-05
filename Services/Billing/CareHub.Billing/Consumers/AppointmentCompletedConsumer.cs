using CareHub.Billing.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Billing.Consumers;

public class AppointmentCompletedConsumer : IConsumer<AppointmentCompleted>
{
    private readonly InvoiceService _invoices;

    public AppointmentCompletedConsumer(InvoiceService invoices) => _invoices = invoices;

    public Task Consume(ConsumeContext<AppointmentCompleted> context) =>
        _invoices.CreateFromAppointmentCompletedAsync(context.Message, context.CancellationToken);
}
