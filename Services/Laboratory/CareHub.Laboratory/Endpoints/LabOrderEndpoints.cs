using System.Security.Claims;
using CareHub.Laboratory.Exceptions;
using CareHub.Laboratory.Models;
using CareHub.Laboratory.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Laboratory.Endpoints;

public static class LabOrderEndpoints
{
    private static readonly string[] ReadRoles = ["LabTechnician", "Doctor", "Manager", "Admin", "Auditor"];
    private static readonly string[] MutationRoles = ["LabTechnician", "Manager", "Admin"];
    private static readonly string[] CreateRoles = ["LabTechnician", "Manager", "Admin"];

    public static IEndpointRouteBuilder MapLabOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/lab-orders", ListAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));
        api.MapGet("/lab-orders/{id:guid}", GetAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));
        api.MapPost("/lab-orders", CreateAsync)
            .RequireAuthorization(p => p.RequireRole(CreateRoles));
        api.MapPost("/lab-orders/{id:guid}/receive-sample", ReceiveSampleAsync)
            .RequireAuthorization(p => p.RequireRole(MutationRoles));
        api.MapPost("/lab-orders/{id:guid}/result", EnterResultAsync)
            .RequireAuthorization(p => p.RequireRole(MutationRoles));

        return app;
    }

    private static Guid UserId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("sub")
            ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Guid CallerBranchId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("branch_id") ?? Guid.Empty.ToString());

    private static bool CanCreateForAnyBranch(ClaimsPrincipal user) =>
        user.IsInRole("Manager") || user.IsInRole("Admin");

    private static async Task<IResult> ListAsync(
        HttpContext http,
        LabOrderService svc,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool global = false,
        CancellationToken ct = default)
    {
        LabOrderStatus? st = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<LabOrderStatus>(status, ignoreCase: true, out var parsed))
            st = parsed;

        var list = await svc.ListAsync(
            patientId,
            branchId,
            st,
            from,
            to,
            global,
            CallerBranchId(http),
            ct);
        return Results.Ok(list);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext http,
        LabOrderService svc,
        [FromQuery] bool global = false,
        CancellationToken ct = default)
    {
        var row = await svc.GetAsync(id, global, CallerBranchId(http), ct);
        return row is null ? Results.NotFound() : Results.Ok(row);
    }

    private static async Task<IResult> CreateAsync(
        CreateLabOrderRequest request,
        HttpContext http,
        LabOrderService svc,
        CancellationToken ct = default)
    {
        try
        {
            var created = await svc.CreateManualAsync(
                request,
                CallerBranchId(http),
                CanCreateForAnyBranch(http.User),
                ct);
            return Results.Created($"/api/lab-orders/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ReceiveSampleAsync(
        Guid id,
        HttpContext http,
        LabOrderService svc,
        CancellationToken ct = default)
    {
        try
        {
            var updated = await svc.MarkSampleReceivedAsync(id, CallerBranchId(http), ct);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidLabOrderStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> EnterResultAsync(
        Guid id,
        EnterLabResultRequest request,
        HttpContext http,
        LabOrderService svc,
        CancellationToken ct = default)
    {
        try
        {
            var updated = await svc.EnterResultAsync(id, request, UserId(http), CallerBranchId(http), ct);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidLabOrderStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
