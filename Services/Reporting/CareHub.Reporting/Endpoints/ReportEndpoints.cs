using System.Security.Claims;
using CareHub.Reporting.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Reporting.Endpoints;

public static class ReportEndpoints
{
    private static readonly string[] ReportRoles = ["Manager", "Admin", "Auditor"];

    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reports")
            .RequireAuthorization(p => p.RequireRole(ReportRoles));

        g.MapGet("/visits", VisitsAsync);
        g.MapGet("/revenue", RevenueAsync);
        g.MapGet("/workload", WorkloadAsync);
        g.MapGet("/cancellations", CancellationsAsync);

        return app;
    }

    private static Guid CallerBranch(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("branch_id") ?? Guid.Empty.ToString());

    private static bool CanUseGlobal(ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Auditor");

    private static bool TryResolveBranchFilter(
        HttpContext http,
        bool global,
        Guid? branchIdQuery,
        out Guid? effectiveBranchFilter,
        out IResult? error)
    {
        error = null;
        var caller = CallerBranch(http);

        if (global && !CanUseGlobal(http.User))
        {
            effectiveBranchFilter = null;
            error = Results.Forbid();
            return false;
        }

        if (!global)
        {
            if (branchIdQuery.HasValue && branchIdQuery.Value != caller && !CanUseGlobal(http.User))
            {
                effectiveBranchFilter = null;
                error = Results.Forbid();
                return false;
            }

            effectiveBranchFilter = branchIdQuery ?? caller;
            return true;
        }

        effectiveBranchFilter = branchIdQuery;
        return true;
    }

    private static IResult? ValidateRange(DateTime? from, DateTime? to)
    {
        if (!from.HasValue || !to.HasValue)
            return Results.BadRequest(new { error = "Query parameters 'from' and 'to' are required (UTC)." });
        if (from.Value > to.Value)
            return Results.BadRequest(new { error = "Parameter 'from' must be less than or equal to 'to'." });
        return null;
    }

    private static async Task<IResult> VisitsAsync(
        HttpContext http,
        ReportQueryService queries,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? doctorId,
        [FromQuery] bool global = false,
        [FromQuery] int maxRows = 500,
        CancellationToken ct = default)
    {
        var bad = ValidateRange(from, to);
        if (bad is not null) return bad;
        if (!TryResolveBranchFilter(http, global, branchId, out var branchFilter, out var err))
            return err!;
        var report = await queries.GetVisitsAsync(from!.Value, to!.Value, branchFilter, doctorId, maxRows, ct);
        return Results.Ok(report);
    }

    private static async Task<IResult> RevenueAsync(
        HttpContext http,
        ReportQueryService queries,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        [FromQuery] bool global = false,
        [FromQuery] int maxRows = 500,
        CancellationToken ct = default)
    {
        var bad = ValidateRange(from, to);
        if (bad is not null) return bad;
        if (!TryResolveBranchFilter(http, global, branchId, out var branchFilter, out var err))
            return err!;
        var report = await queries.GetRevenueAsync(from!.Value, to!.Value, branchFilter, maxRows, ct);
        return Results.Ok(report);
    }

    private static async Task<IResult> WorkloadAsync(
        HttpContext http,
        ReportQueryService queries,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? doctorId,
        [FromQuery] bool global = false,
        [FromQuery] int maxRows = 500,
        CancellationToken ct = default)
    {
        var bad = ValidateRange(from, to);
        if (bad is not null) return bad;
        if (!TryResolveBranchFilter(http, global, branchId, out var branchFilter, out var err))
            return err!;
        var report = await queries.GetWorkloadAsync(from!.Value, to!.Value, branchFilter, doctorId, maxRows, ct);
        return Results.Ok(report);
    }

    private static async Task<IResult> CancellationsAsync(
        HttpContext http,
        ReportQueryService queries,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? doctorId,
        [FromQuery] bool global = false,
        [FromQuery] int maxRows = 500,
        CancellationToken ct = default)
    {
        var bad = ValidateRange(from, to);
        if (bad is not null) return bad;
        if (!TryResolveBranchFilter(http, global, branchId, out var branchFilter, out var err))
            return err!;
        var report = await queries.GetCancellationsAsync(from!.Value, to!.Value, branchFilter, doctorId, maxRows, ct);
        return Results.Ok(report);
    }
}
