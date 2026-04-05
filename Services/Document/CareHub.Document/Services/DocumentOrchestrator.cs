using System.Security.Cryptography;
using System.Text;
using CareHub.Document.Clients;
using QuestPDF.Fluent;
using CareHub.Document.Data;
using CareHub.Document.Models;
using CareHub.Document.Options;
using CareHub.Document.Pdf;
using CareHub.Document.Storage;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Laboratory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CareHub.Document.Services;

public class DocumentOrchestrator
{
    private readonly DocumentDbContext _db;
    private readonly IDocumentStorage _storage;
    private readonly IInvoicePdfRenderer _invoicePdf;
    private readonly ILabResultPdfRenderer _labPdf;
    private readonly ILaboratoryInternalClient _labClient;
    private readonly IOptionsMonitor<DocumentFeatureOptions> _docFeatures;
    private readonly ILogger<DocumentOrchestrator> _log;

    public DocumentOrchestrator(
        DocumentDbContext db,
        IDocumentStorage storage,
        IInvoicePdfRenderer invoicePdf,
        ILabResultPdfRenderer labPdf,
        ILaboratoryInternalClient labClient,
        IOptionsMonitor<DocumentFeatureOptions> docFeatures,
        ILogger<DocumentOrchestrator> log)
    {
        _db = db;
        _storage = storage;
        _invoicePdf = invoicePdf;
        _labPdf = labPdf;
        _labClient = labClient;
        _docFeatures = docFeatures;
        _log = log;
    }

    public Task HandleInvoiceGeneratedAsync(InvoiceGenerated msg, CancellationToken ct) =>
        PersistPdfAsync(
            DocumentKind.Invoice,
            "Invoice",
            msg.InvoiceId,
            msg.BranchId,
            DocumentSource.Event,
            $"invoice-{msg.InvoiceId:N}.pdf",
            _ => Task.FromResult(_invoicePdf.Render(msg)),
            ct);

    public async Task HandleLabResultReadyAsync(LabResultReady msg, CancellationToken ct)
    {
        var fallback = _docFeatures.CurrentValue.LaboratoryFallbackMessage;
        LaboratoryDocumentContextDto? ctx = null;
        try
        {
            ctx = await _labClient.GetLabOrderDocumentContextAsync(msg.LabOrderId, ct);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Laboratory internal API failed for lab order {LabOrderId}", msg.LabOrderId);
        }

        var useFallback = ctx is null || string.IsNullOrWhiteSpace(ctx.ResultSummary);
        var model = new LabResultPdfModel
        {
            LabOrderId = msg.LabOrderId,
            AppointmentId = msg.AppointmentId,
            PatientId = msg.PatientId,
            DoctorId = msg.DoctorId,
            ResultSummary = ctx?.ResultSummary,
            UseFallbackNotice = useFallback,
            FallbackMessage = fallback,
        };

        await PersistPdfAsync(
            DocumentKind.LabResult,
            "LabOrder",
            msg.LabOrderId,
            ctx?.BranchId,
            DocumentSource.Event,
            $"lab-result-{msg.LabOrderId:N}.pdf",
            _ => Task.FromResult(_labPdf.Render(model)),
            ct);
    }

    public async Task<(StoredDocument Doc, bool Created)> GenerateReferralStubAsync(
        Guid entityId,
        Guid? branchId,
        CancellationToken ct)
    {
        var existing = await _db.StoredDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Kind == DocumentKind.Referral && d.EntityId == entityId, ct);
        if (existing != null)
            return (existing, false);

        var row = await PersistPdfAsync(
            DocumentKind.Referral,
            "Referral",
            entityId,
            branchId,
            DocumentSource.OnDemand,
            $"referral-{entityId:N}.pdf",
            _ => Task.FromResult(RenderReferralStubPdf(entityId, branchId)),
            ct);

        if (row != null)
            return (row, true);

        existing = await _db.StoredDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Kind == DocumentKind.Referral && d.EntityId == entityId, ct);
        return (existing!, false);
    }

    private static byte[] RenderReferralStubPdf(Guid entityId, Guid? branchId)
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
        return global::QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Text("Referral (stub)").SemiBold().FontSize(18);
                    col.Item().Text($"Referral ID: {entityId:D}");
                    if (branchId is { } b)
                        col.Item().Text($"Branch: {b:D}");
                    col.Item().PaddingTop(12).Text("This is a placeholder document until referral workflows publish domain events.");
                });
            });
        }).GeneratePdf();
    }

    private async Task<StoredDocument?> PersistPdfAsync(
        DocumentKind kind,
        string entityType,
        Guid entityId,
        Guid? branchId,
        DocumentSource source,
        string fileName,
        Func<CancellationToken, Task<byte[]>> buildPdf,
        CancellationToken ct)
    {
        if (await _db.StoredDocuments.AsNoTracking()
                .AnyAsync(d => d.Kind == kind && d.EntityId == entityId, ct))
            return null;

        var bytes = await buildPdf(ct);
        var sha = ToHexSha256(bytes);
        var id = Guid.NewGuid();
        var storageKey = $"{kind.ToString().ToLowerInvariant()}/{entityId:D}/{id:N}.pdf";

        await _storage.WriteAsync(storageKey, bytes, ct);

        var row = new StoredDocument
        {
            Id = id,
            Kind = kind,
            FileName = fileName,
            ContentType = "application/pdf",
            StorageKey = storageKey,
            Sha256 = sha,
            EntityType = entityType,
            EntityId = entityId,
            BranchId = branchId,
            CreatedAt = DateTime.UtcNow,
            Source = source,
        };

        _db.StoredDocuments.Add(row);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex)
        {
            if (await _db.StoredDocuments.AsNoTracking()
                    .AnyAsync(d => d.Kind == kind && d.EntityId == entityId, ct))
            {
                _log.LogInformation(
                    ex,
                    "Skipped duplicate document for {Kind} entity {EntityId}",
                    kind,
                    entityId);
                return null;
            }

            throw;
        }

        return row;
    }

    private static string ToHexSha256(byte[] bytes)
    {
        var hash = SHA256.HashData(bytes);
        var sb = new StringBuilder(hash.Length * 2);
        foreach (var b in hash)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }
}
