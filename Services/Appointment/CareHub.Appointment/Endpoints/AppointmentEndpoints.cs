using System.Net.Http;
using System.Security.Claims;
using CareHub.Appointment.Exceptions;
using CareHub.Appointment.Models;
using CareHub.Appointment.Services;
using Microsoft.AspNetCore.Mvc;

namespace CareHub.Appointment.Endpoints;

public static class AppointmentEndpoints
{
    private static readonly string[] ListRoles = ["Doctor", "Manager", "Receptionist", "Admin", "Auditor"];
    private static readonly string[] BookingRoles = ["Receptionist", "Admin", "Manager"];
    private static readonly string[] CheckInRoles = ["Receptionist", "Admin"];
    private static readonly string[] CompleteRoles = ["Doctor", "Admin"];

    public static IEndpointRouteBuilder MapAppointmentEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/appointments", ListAsync)
            .RequireAuthorization(p => p.RequireRole(ListRoles));
        api.MapGet("/appointments/{id:guid}", GetAsync)
            .RequireAuthorization(p => p.RequireRole(ListRoles));
        api.MapPost("/appointments", CreateAsync)
            .RequireAuthorization(p => p.RequireRole(BookingRoles));
        api.MapPut("/appointments/{id:guid}", RescheduleAsync)
            .RequireAuthorization(p => p.RequireRole(BookingRoles));
        api.MapPost("/appointments/{id:guid}/cancel", CancelAsync)
            .RequireAuthorization(p => p.RequireRole(BookingRoles));
        api.MapPost("/appointments/{id:guid}/checkin", CheckInAsync)
            .RequireAuthorization(p => p.RequireRole(CheckInRoles));
        api.MapPost("/appointments/{id:guid}/complete", CompleteAsync)
            .RequireAuthorization(p => p.RequireRole(CompleteRoles));

        return app;
    }

    private static string? Bearer(HttpContext http) =>
        http.Request.Headers.Authorization.ToString();

    private static Guid UserId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("sub")
            ?? http.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private static Guid CallerBranchId(HttpContext http) =>
        Guid.Parse(http.User.FindFirstValue("branch_id") ?? Guid.Empty.ToString());

    private static async Task<IResult> ListAsync(
        HttpContext http,
        AppointmentService svc,
        [FromQuery] Guid? patientId,
        [FromQuery] Guid? doctorId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool global = false)
    {
        AppointmentStatus? st = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<AppointmentStatus>(status, ignoreCase: true, out var parsed))
            st = parsed;

        var callerBranchId = CallerBranchId(http);
        var list = await svc.ListAsync(patientId, doctorId, branchId, st, from, to, global, callerBranchId);
        return Results.Ok(list);
    }

    private static async Task<IResult> GetAsync(
        Guid id,
        HttpContext http,
        AppointmentService svc,
        CancellationToken ct,
        [FromQuery] bool global = false)
    {
        var row = await svc.GetAsync(id, global, CallerBranchId(http), ct);
        return row is null ? Results.NotFound() : Results.Ok(row);
    }

    private static async Task<IResult> CreateAsync(
        CreateAppointmentRequest request,
        HttpContext http,
        AppointmentService svc)
    {
        try
        {
            var created = await svc.CreateAsync(request, UserId(http), Bearer(http));
            return Results.Created($"/api/appointments/{created.Id}", created);
        }
        catch (KeyNotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (SlotValidationFailedException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (AppointmentOverlapException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> RescheduleAsync(
        Guid id,
        RescheduleAppointmentRequest request,
        HttpContext http,
        AppointmentService svc)
    {
        try
        {
            var updated = await svc.RescheduleAsync(id, request, UserId(http), Bearer(http));
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (SlotValidationFailedException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (InvalidAppointmentStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (AppointmentOverlapException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<IResult> CancelAsync(
        Guid id,
        CancelAppointmentRequest request,
        HttpContext http,
        AppointmentService svc)
    {
        try
        {
            var updated = await svc.CancelAsync(id, request, UserId(http));
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidAppointmentStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CheckInAsync(Guid id, AppointmentService svc)
    {
        try
        {
            var updated = await svc.CheckInAsync(id);
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidAppointmentStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> CompleteAsync(
        Guid id,
        CompleteAppointmentRequest request,
        HttpContext http,
        AppointmentService svc)
    {
        try
        {
            var updated = await svc.CompleteAsync(id, request, UserId(http));
            return Results.Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
        catch (InvalidAppointmentStateException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }
}
