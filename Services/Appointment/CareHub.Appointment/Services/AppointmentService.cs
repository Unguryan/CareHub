using CareHub.Appointment.Data;
using CareHub.Appointment.Events;
using CareHub.Appointment.Exceptions;
using CareHub.Appointment.Models;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Appointment.Services;

public class AppointmentService
{
    private readonly AppointmentDbContext _db;
    private readonly AppointmentEventPublisher _events;
    private readonly ScheduleSlotClient _schedule;
    private readonly PatientClient _patients;

    public AppointmentService(
        AppointmentDbContext db,
        AppointmentEventPublisher events,
        ScheduleSlotClient schedule,
        PatientClient patients)
    {
        _db = db;
        _events = events;
        _schedule = schedule;
        _patients = patients;
    }

    public async Task<AppointmentResponse> CreateAsync(
        CreateAppointmentRequest request,
        Guid userId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        var scheduledAtUtc = AsUtc(request.ScheduledAt);

        await _patients.EnsurePatientExistsAsync(request.PatientId, bearerToken, ct);
        await _schedule.EnsureSlotIsValidAsync(
            request.DoctorId, scheduledAtUtc, request.DurationMinutes, bearerToken, ct);

        await ThrowIfOverlapAsync(
            request.DoctorId,
            scheduledAtUtc,
            request.DurationMinutes,
            excludeId: null,
            ct);

        var now = DateTime.UtcNow;
        var entity = new global::CareHub.Appointment.Models.Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            BranchId = request.BranchId,
            ScheduledAt = scheduledAtUtc,
            DurationMinutes = request.DurationMinutes,
            Status = AppointmentStatus.Scheduled,
            RequiresLabWork = false,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedByUserId = userId,
        };

        _db.Appointments.Add(entity);
        await _db.SaveChangesAsync(ct);
        await _events.PublishCreatedAsync(entity, userId);
        return AppointmentResponse.FromEntity(entity);
    }

    public async Task<List<AppointmentResponse>> ListAsync(
        Guid? patientId,
        Guid? doctorId,
        Guid? branchId,
        AppointmentStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool global,
        Guid callerBranchId,
        CancellationToken ct = default)
    {
        var q = _db.Appointments.AsQueryable();

        if (!global && branchId is null)
            q = q.Where(a => a.BranchId == callerBranchId);
        else if (branchId.HasValue)
            q = q.Where(a => a.BranchId == branchId.Value);

        if (patientId.HasValue) q = q.Where(a => a.PatientId == patientId.Value);
        if (doctorId.HasValue) q = q.Where(a => a.DoctorId == doctorId.Value);
        if (status.HasValue) q = q.Where(a => a.Status == status.Value);
        if (fromUtc.HasValue) q = q.Where(a => a.ScheduledAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(a => a.ScheduledAt <= toUtc.Value);

        var rows = await q
            .OrderBy(a => a.ScheduledAt)
            .ToListAsync(ct);
        return rows.ConvertAll(AppointmentResponse.FromEntity);
    }

    public async Task<AppointmentResponse?> GetAsync(Guid id, bool global, Guid callerBranchId, CancellationToken ct)
    {
        var a = await _db.Appointments.FindAsync([id], ct);
        if (a is null) return null;
        if (!global && a.BranchId != callerBranchId) return null;
        return AppointmentResponse.FromEntity(a);
    }

    public async Task<AppointmentResponse> RescheduleAsync(
        Guid id,
        RescheduleAppointmentRequest request,
        Guid userId,
        string? bearerToken,
        CancellationToken ct = default)
    {
        var entity = await _db.Appointments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (entity.Status is not AppointmentStatus.Scheduled and not AppointmentStatus.CheckedIn)
            throw new InvalidAppointmentStateException("Only scheduled or checked-in appointments can be rescheduled.");

        var previous = entity.ScheduledAt;
        var duration = request.DurationMinutes ?? entity.DurationMinutes;
        var scheduledAtUtc = AsUtc(request.ScheduledAt);

        await _schedule.EnsureSlotIsValidAsync(entity.DoctorId, scheduledAtUtc, duration, bearerToken, ct);
        await ThrowIfOverlapAsync(entity.DoctorId, scheduledAtUtc, duration, entity.Id, ct);

        entity.ScheduledAt = scheduledAtUtc;
        entity.DurationMinutes = duration;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _events.PublishRescheduledAsync(entity, previous, userId);
        return AppointmentResponse.FromEntity(entity);
    }

    public async Task<AppointmentResponse> CancelAsync(
        Guid id,
        CancelAppointmentRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var entity = await _db.Appointments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (entity.Status is not AppointmentStatus.Scheduled and not AppointmentStatus.CheckedIn)
            throw new InvalidAppointmentStateException("Appointment cannot be cancelled in its current state.");

        entity.Status = AppointmentStatus.Cancelled;
        entity.CancellationReason = request.Reason;
        entity.CancelledByUserId = userId;
        entity.CancelledAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _events.PublishCancelledAsync(entity, userId);
        return AppointmentResponse.FromEntity(entity);
    }

    public async Task<AppointmentResponse> CheckInAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _db.Appointments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (entity.Status != AppointmentStatus.Scheduled)
            throw new InvalidAppointmentStateException("Only scheduled appointments can be checked in.");

        entity.Status = AppointmentStatus.CheckedIn;
        entity.CheckedInAt = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return AppointmentResponse.FromEntity(entity);
    }

    public async Task<AppointmentResponse> CompleteAsync(
        Guid id,
        CompleteAppointmentRequest request,
        Guid userId,
        CancellationToken ct = default)
    {
        var entity = await _db.Appointments.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Appointment {id} not found.");

        if (entity.Status != AppointmentStatus.CheckedIn)
            throw new InvalidAppointmentStateException("Only checked-in appointments can be completed.");

        entity.Status = AppointmentStatus.Completed;
        entity.RequiresLabWork = request.RequiresLabWork;
        entity.CompletedAt = DateTime.UtcNow;
        entity.CompletedByUserId = userId;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        await _events.PublishCompletedAsync(entity, userId);
        return AppointmentResponse.FromEntity(entity);
    }

    private async Task ThrowIfOverlapAsync(
        Guid doctorId,
        DateTime startUtc,
        int durationMinutes,
        Guid? excludeId,
        CancellationToken ct)
    {
        var endUtc = startUtc.AddMinutes(durationMinutes);

        var hasOverlap = await _db.Appointments
            .Where(a => a.DoctorId == doctorId)
            .Where(a => a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.CheckedIn)
            .Where(a => excludeId == null || a.Id != excludeId.Value)
            .Where(a => a.ScheduledAt < endUtc && startUtc < a.ScheduledAt.AddMinutes(a.DurationMinutes))
            .AnyAsync(ct);

        if (hasOverlap)
            throw new AppointmentOverlapException();
    }

    private static DateTime AsUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
