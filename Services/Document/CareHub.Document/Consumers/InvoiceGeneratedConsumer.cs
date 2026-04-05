using CareHub.Document.Services;
using CareHub.Shared.Contracts.Events.Billing;
using MassTransit;

namespace CareHub.Document.Consumers;

public class InvoiceGeneratedConsumer(DocumentOrchestrator orchestrator, ILogger<InvoiceGeneratedConsumer> log)
    : IConsumer<InvoiceGenerated>
{
    public async Task Consume(ConsumeContext<InvoiceGenerated> context)
    {
        try
        {
            await orchestrator.HandleInvoiceGeneratedAsync(context.Message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to process InvoiceGenerated for {InvoiceId}", context.Message.InvoiceId);
            throw;
        }
    }
}
