using CareHub.Document.Data;
using CareHub.Document.Models;
using CareHub.Document.Services;
using CareHub.Document.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Document.Endpoints;

public static class DocumentEndpoints
{
    private static readonly string[] DownloadListRoles =
        ["Admin", "Manager", "Accountant", "Doctor", "LabTechnician", "Receptionist"];

    private static readonly string[] GenerateRoles = ["Admin", "Manager", "Receptionist"];

    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/documents");

        api.MapGet("", ListAsync)
            .RequireAuthorization(p => p.RequireRole(DownloadListRoles));
        api.MapGet("/{id:guid}", DownloadAsync)
            .RequireAuthorization(p => p.RequireRole(DownloadListRoles));
        api.MapPost("/generate", GenerateAsync)
            .RequireAuthorization(p => p.RequireRole(GenerateRoles));

        return app;
    }

    private static async Task<IResult> ListAsync(
        [FromQuery] string entityType,
        [FromQuery] Guid entityId,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        DocumentDbContext db,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entityType) || entityId == Guid.Empty)
            return Results.BadRequest(new { error = "entityType and entityId are required." });
        var p = page is null or < 1 ? 1 : page.Value;
        var ps = pageSize is null or < 1 or > 100 ? 20 : pageSize.Value;

        var q = db.StoredDocuments.AsNoTracking()
            .Where(d => d.EntityType == entityType && d.EntityId == entityId)
            .OrderByDescending(d => d.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((p - 1) * ps).Take(ps)
            .Select(d => new StoredDocumentListItemDto(
                d.Id,
                d.Kind.ToString(),
                d.FileName,
                d.EntityType,
                d.EntityId,
                d.BranchId,
                d.CreatedAt,
                d.Source.ToString()))
            .ToListAsync(ct);

        return Results.Ok(new { page = p, pageSize = ps, total, items });
    }

    private static async Task<IResult> DownloadAsync(
        Guid id,
        DocumentDbContext db,
        IDocumentStorage storage,
        CancellationToken ct)
    {
        var doc = await db.StoredDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null)
            return Results.NotFound();

        await using var stream = await storage.OpenReadAsync(doc.StorageKey, ct);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        return Results.File(ms.ToArray(), doc.ContentType, doc.FileName);
    }

    private static async Task<IResult> GenerateAsync(
        GenerateDocumentRequest body,
        DocumentOrchestrator orchestrator,
        CancellationToken ct)
    {
        if (body is null || string.IsNullOrWhiteSpace(body.Template))
            return Results.BadRequest(new { error = "Template is required." });
        if (body.EntityId == Guid.Empty)
            return Results.BadRequest(new { error = "entityId is required." });

        if (!string.Equals(body.Template, "Referral", StringComparison.OrdinalIgnoreCase))
            return Results.BadRequest(new { error = "Only template 'Referral' is supported in this phase." });

        var (doc, created) = await orchestrator.GenerateReferralStubAsync(body.EntityId, body.BranchId, ct);
        var location = $"/api/documents/{doc.Id}";
        return created
            ? Results.Created(location, ToDto(doc))
            : Results.Ok(ToDto(doc));
    }

    private static StoredDocumentListItemDto ToDto(StoredDocument d) =>
        new(d.Id, d.Kind.ToString(), d.FileName, d.EntityType, d.EntityId, d.BranchId, d.CreatedAt, d.Source.ToString());
}

public sealed record StoredDocumentListItemDto(
    Guid Id,
    string Kind,
    string FileName,
    string EntityType,
    Guid EntityId,
    Guid? BranchId,
    DateTime CreatedAt,
    string Source);

public sealed record GenerateDocumentRequest(string Template, Guid EntityId, Guid? BranchId);
