using System.Security.Claims;
using CareHub.Billing.Exceptions;
using CareHub.Billing.Models;
using CareHub.Billing.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Billing.Endpoints;

public static class InvoiceEndpoints
{
    private static readonly string[] ReadRoles = ["Accountant", "Manager", "Admin", "Auditor"];
    private static readonly string[] PayRefundRoles = ["Accountant", "Admin"];

    public static IEndpointRouteBuilder MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/invoices", ListAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));
        api.MapGet("/invoices/{id:guid}", GetAsync)
            .RequireAuthorization(p => p.RequireRole(ReadRoles));
        api.MapPost("/invoices/{id:guid}/pay", PayAsync)
            .RequireAuthorization(p => p.RequireRole(PayRefundRoles));
        api.MapPost("/invoices/{id:guid}/refund", RefundAsync)
            .RequireAuthorization(p => p.RequireRole(PayRefundRoles));

        return app;
    }

    private static Guid UserId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("sub")
            ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Guid CallerBranchId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("branch_id") ?? Guid.Empty.ToString());

    private static async Task<IResult> ListAsync(
        HttpContext http,
        InvoiceService svc,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool global = false,
        CancellationToken ct = default)
    {
        InvoiceStatus? st = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<InvoiceStatus>(status, ignoreCase: true, out var parsed))
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
        InvoiceService svc,
        [FromQuery] bool global = false,
        CancellationToken ct = default)
    {
        var row = await svc.GetAsync(id, global, CallerBranchId(http), ct);
        return row is null ? Results.NotFound() : Results.Ok(row);
    }

    private static async Task<IResult> PayAsync(
        Guid id,
        HttpContext http,
        InvoiceService svc,
        CancellationToken ct = default)
    {
        try
        {
            var updated = await svc.MarkPaidAsync(id, UserId(http), CallerBranchId(http), ct);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidInvoiceStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RefundAsync(
        Guid id,
        RefundInvoiceRequest request,
        HttpContext http,
        InvoiceService svc,
        CancellationToken ct = default)
    {
        try
        {
            var updated = await svc.RefundAsync(id, request, UserId(http), CallerBranchId(http), ct);
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
        catch (InvalidInvoiceStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
