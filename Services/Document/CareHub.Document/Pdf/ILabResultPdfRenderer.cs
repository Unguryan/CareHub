namespace CareHub.Document.Pdf;

public interface ILabResultPdfRenderer
{
    byte[] Render(LabResultPdfModel model);
}
