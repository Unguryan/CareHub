using CareHub.Shared.Contracts.Events.Billing;

namespace CareHub.Document.Pdf;

public interface IInvoicePdfRenderer
{
    byte[] Render(InvoiceGenerated invoice);
}
