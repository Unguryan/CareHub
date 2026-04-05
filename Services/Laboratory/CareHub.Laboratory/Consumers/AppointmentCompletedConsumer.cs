using CareHub.Laboratory.Services;
using CareHub.Shared.Contracts.Events.Appointments;
using MassTransit;

namespace CareHub.Laboratory.Consumers;

public class AppointmentCompletedConsumer : IConsumer<AppointmentCompleted>
{
    private readonly LabOrderService _orders;

    public AppointmentCompletedConsumer(LabOrderService orders) => _orders = orders;

    public Task Consume(ConsumeContext<AppointmentCompleted> context) =>
        _orders.CreateFromAppointmentCompletedAsync(context.Message, context.CancellationToken);
}
