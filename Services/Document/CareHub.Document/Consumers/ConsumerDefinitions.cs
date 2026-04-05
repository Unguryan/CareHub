using MassTransit;

namespace CareHub.Document.Consumers;

public class InvoiceGeneratedConsumerDefinition : ConsumerDefinition<InvoiceGeneratedConsumer>
{
    public InvoiceGeneratedConsumerDefinition() => EndpointName = "document-invoice-generated";
}

public class LabResultReadyConsumerDefinition : ConsumerDefinition<LabResultReadyConsumer>
{
    public LabResultReadyConsumerDefinition() => EndpointName = "document-lab-result-ready";
}
