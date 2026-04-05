using CareHub.Document.Services;
using CareHub.Shared.Contracts.Events.Laboratory;
using MassTransit;

namespace CareHub.Document.Consumers;

public class LabResultReadyConsumer(DocumentOrchestrator orchestrator, ILogger<LabResultReadyConsumer> log)
    : IConsumer<LabResultReady>
{
    public async Task Consume(ConsumeContext<LabResultReady> context)
    {
        try
        {
            await orchestrator.HandleLabResultReadyAsync(context.Message, context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to process LabResultReady for {LabOrderId}", context.Message.LabOrderId);
            throw;
        }
    }
}
