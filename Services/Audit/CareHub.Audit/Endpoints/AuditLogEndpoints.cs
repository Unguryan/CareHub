using CareHub.Audit.Data;
using CareHub.Audit.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Audit.Endpoints;

public static class AuditLogEndpoints
{
    private static readonly string[] ReadRoles = ["Admin", "Auditor"];

    public static IEndpointRouteBuilder MapAuditLogEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/audit-logs", ListAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));
        api.MapGet("/audit-logs/{id:guid}", GetByIdAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));

        return app;
    }

    private static async Task<IResult> ListAsync(
        AuditDbContext db,
        [FromQuery] Guid? userId,
        [FromQuery] string? actionType,
        [FromQuery] string? entityType,
        [FromQuery] Guid? entityId,
        [FromQuery] Guid? branchId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] DateTime? cursorRecordedAt,
        [FromQuery] Guid? cursorId,
        [FromQuery] int? pageSize,
        CancellationToken ct = default)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return Results.BadRequest(new { error = "Parameter 'from' must be less than or equal to 'to'." });

        var hasCursorRecordedAt = cursorRecordedAt.HasValue;
        var hasCursorId = cursorId.HasValue;
        if (hasCursorRecordedAt != hasCursorId)
            return Results.BadRequest(new { error = "Both cursorRecordedAt and cursorId are required for pagination." });

        var size = pageSize ?? 20;
        if (size < 1) size = 1;
        if (size > 100) size = 100;

        IQueryable<AuditLogEntry> q = db.AuditLogEntries.AsNoTracking();

        if (userId.HasValue)
            q = q.Where(e => e.ActorUserId == userId);
        if (!string.IsNullOrEmpty(actionType))
            q = q.Where(e => e.ActionType == actionType);
        if (!string.IsNullOrEmpty(entityType))
            q = q.Where(e => e.EntityType == entityType);
        if (entityId.HasValue)
            q = q.Where(e => e.EntityId == entityId);
        if (branchId.HasValue)
            q = q.Where(e => e.BranchId == branchId);
        if (from.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(from.Value, DateTimeKind.Utc);
            q = q.Where(e => e.RecordedAt >= fromUtc);
        }

        if (to.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(to.Value, DateTimeKind.Utc);
            q = q.Where(e => e.RecordedAt <= toUtc);
        }

        if (hasCursorRecordedAt && cursorId.HasValue)
        {
            var cr = DateTime.SpecifyKind(cursorRecordedAt!.Value, DateTimeKind.Utc);
            var cid = cursorId.Value;
            q = q.Where(e => e.RecordedAt < cr || (e.RecordedAt == cr && e.Id.CompareTo(cid) < 0));
        }

        q = q.OrderByDescending(e => e.RecordedAt).ThenByDescending(e => e.Id);

        var take = size + 1;
        var rows = await q.Take(take).ToListAsync(ct);
        var hasMore = rows.Count > size;
        if (hasMore)
            rows.RemoveAt(rows.Count - 1);

        DateTime? nextRecordedAt = null;
        Guid? nextId = null;
        if (hasMore && rows.Count > 0)
        {
            var last = rows[^1];
            nextRecordedAt = last.RecordedAt;
            nextId = last.Id;
        }

        var items = rows.Select(e => new AuditLogSummaryDto(
            e.Id,
            e.RecordedAt,
            e.ActionType,
            e.ActorUserId,
            e.EntityType,
            e.EntityId,
            e.BranchId)).ToList();

        return Results.Ok(new AuditLogListResponse(items, hasMore, nextRecordedAt, nextId));
    }

    private static async Task<IResult> GetByIdAsync(Guid id, AuditDbContext db, CancellationToken ct)
    {
        var e = await db.AuditLogEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e is null)
            return Results.NotFound();

        var dto = new AuditLogDetailDto(
            e.Id,
            e.RecordedAt,
            e.ActionType,
            e.ActorUserId,
            e.EntityType,
            e.EntityId,
            e.BranchId,
            e.DetailsJson);
        return Results.Ok(dto);
    }
}

public record AuditLogSummaryDto(
    Guid Id,
    DateTime RecordedAt,
    string ActionType,
    Guid? ActorUserId,
    string? EntityType,
    Guid? EntityId,
    Guid? BranchId);

public record AuditLogListResponse(
    IReadOnlyList<AuditLogSummaryDto> Items,
    bool HasMore,
    DateTime? NextCursorRecordedAt,
    Guid? NextCursorId);

public record AuditLogDetailDto(
    Guid Id,
    DateTime RecordedAt,
    string ActionType,
    Guid? ActorUserId,
    string? EntityType,
    Guid? EntityId,
    Guid? BranchId,
    string DetailsJson);
