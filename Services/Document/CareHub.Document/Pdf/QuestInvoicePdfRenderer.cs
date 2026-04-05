using CareHub.Shared.Contracts.Events.Billing;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CareHub.Document.Pdf;

public sealed class QuestInvoicePdfRenderer : IInvoicePdfRenderer
{
    public QuestInvoicePdfRenderer() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Render(InvoiceGenerated invoice) =>
        global::QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("CareHub").SemiBold().FontSize(10).FontColor("#2563eb");
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Invoice").SemiBold().FontSize(20);
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().Text($"Invoice ID: {invoice.InvoiceId:D}");
                    col.Item().Text($"Appointment: {invoice.AppointmentId:D}");
                    col.Item().Text($"Patient: {invoice.PatientId:D}");
                    col.Item().Text($"Branch: {invoice.BranchId:D}");
                    col.Item().Text($"Amount: {invoice.Amount:N2} {invoice.Currency}");
                    col.Item().Text($"Issued (UTC): {invoice.OccurredAt:u}");
                });
            });
        }).GeneratePdf();
}
