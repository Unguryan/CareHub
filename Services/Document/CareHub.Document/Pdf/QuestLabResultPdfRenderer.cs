using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace CareHub.Document.Pdf;

public sealed class QuestLabResultPdfRenderer : ILabResultPdfRenderer
{
    public QuestLabResultPdfRenderer() =>
        QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Render(LabResultPdfModel model) =>
        global::QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Header().Text("CareHub Laboratory").SemiBold().FontSize(10).FontColor("#2563eb");
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text("Lab result summary").SemiBold().FontSize(18);
                    col.Item().LineHorizontal(1).LineColor("#e5e7eb");
                    col.Item().Text($"Lab order: {model.LabOrderId:D}");
                    col.Item().Text($"Appointment: {model.AppointmentId:D}");
                    col.Item().Text($"Patient: {model.PatientId:D}");
                    col.Item().Text($"Ordering physician: {model.DoctorId:D}");
                    if (model.UseFallbackNotice)
                    {
                        col.Item().PaddingTop(12).Text(model.FallbackMessage).Italic();
                    }
                    else if (!string.IsNullOrWhiteSpace(model.ResultSummary))
                    {
                        col.Item().PaddingTop(8).Text("Result").SemiBold();
                        col.Item().Text(model.ResultSummary);
                    }
                });
            });
        }).GeneratePdf();
}
