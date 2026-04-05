using CareHub.Laboratory.Data;
using CareHub.Laboratory.Events;
using CareHub.Laboratory.Exceptions;
using CareHub.Laboratory.Models;
using CareHub.Shared.Contracts.Events.Appointments;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Laboratory.Services;

public class LabOrderService
{
    private readonly LaboratoryDbContext _db;
    private readonly LaboratoryEventPublisher _events;

    public LabOrderService(LaboratoryDbContext db, LaboratoryEventPublisher events)
    {
        _db = db;
        _events = events;
    }

    public async Task CreateFromAppointmentCompletedAsync(AppointmentCompleted msg, CancellationToken ct = default)
    {
        if (!msg.RequiresLabWork)
            return;

        if (await _db.LabOrders.AnyAsync(o => o.AppointmentId == msg.AppointmentId, ct))
            return;

        var now = DateTime.UtcNow;
        var order = new LabOrder
        {
            Id = Guid.NewGuid(),
            AppointmentId = msg.AppointmentId,
            PatientId = msg.PatientId,
            DoctorId = msg.DoctorId,
            BranchId = msg.BranchId,
            Status = LabOrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LabOrders.Add(order);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.LabOrders.AnyAsync(o => o.AppointmentId == msg.AppointmentId, ct))
                return;
            throw;
        }
    }

    public async Task<LabOrderResponse> CreateManualAsync(
        CreateLabOrderRequest request,
        Guid callerBranchId,
        bool canCreateForAnyBranch,
        CancellationToken ct = default)
    {
        if (!canCreateForAnyBranch && request.BranchId != callerBranchId)
            throw new ArgumentException("BranchId must match your assigned branch.");

        if (await _db.LabOrders.AnyAsync(o => o.AppointmentId == request.AppointmentId, ct))
            throw new InvalidOperationException($"A lab order for appointment {request.AppointmentId} already exists.");

        var now = DateTime.UtcNow;
        var order = new LabOrder
        {
            Id = Guid.NewGuid(),
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            DoctorId = request.DoctorId,
            BranchId = request.BranchId,
            Status = LabOrderStatus.Pending,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.LabOrders.Add(order);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.LabOrders.AnyAsync(o => o.AppointmentId == request.AppointmentId, ct))
                throw new InvalidOperationException($"A lab order for appointment {request.AppointmentId} already exists.");
            throw;
        }

        return LabOrderResponse.FromEntity(order);
    }

    public async Task<List<LabOrderResponse>> ListAsync(
        Guid? patientId,
        Guid? branchId,
        LabOrderStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool global,
        Guid callerBranchId,
        CancellationToken ct = default)
    {
        var q = _db.LabOrders.AsQueryable();

        if (!global && branchId is null)
            q = q.Where(o => o.BranchId == callerBranchId);
        else if (branchId.HasValue)
            q = q.Where(o => o.BranchId == branchId.Value);

        if (patientId.HasValue) q = q.Where(o => o.PatientId == patientId.Value);
        if (status.HasValue) q = q.Where(o => o.Status == status.Value);
        if (fromUtc.HasValue) q = q.Where(o => o.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(o => o.CreatedAt <= toUtc.Value);

        var rows = await q.OrderByDescending(o => o.CreatedAt).ToListAsync(ct);
        return rows.ConvertAll(LabOrderResponse.FromEntity);
    }

    public async Task<LabOrderResponse?> GetAsync(Guid id, bool global, Guid callerBranchId, CancellationToken ct)
    {
        var o = await _db.LabOrders.FindAsync([id], ct);
        if (o is null) return null;
        if (!global && o.BranchId != callerBranchId) return null;
        return LabOrderResponse.FromEntity(o);
    }

    public async Task<LabOrderResponse> MarkSampleReceivedAsync(Guid id, Guid callerBranchId, CancellationToken ct = default)
    {
        var order = await _db.LabOrders.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Lab order {id} not found.");
        if (order.BranchId != callerBranchId)
            throw new KeyNotFoundException($"Lab order {id} not found.");
        if (order.Status != LabOrderStatus.Pending)
            throw new InvalidLabOrderStateException("Only pending orders can receive a sample.");

        var now = DateTime.UtcNow;
        order.Status = LabOrderStatus.SampleReceived;
        order.SampleReceivedAt = now;
        order.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        return LabOrderResponse.FromEntity(order);
    }

    public async Task<LabOrderResponse> EnterResultAsync(
        Guid id,
        EnterLabResultRequest request,
        Guid userId,
        Guid callerBranchId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Summary))
            throw new ArgumentException("Summary is required.", nameof(request));

        var order = await _db.LabOrders.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Lab order {id} not found.");
        if (order.BranchId != callerBranchId)
            throw new KeyNotFoundException($"Lab order {id} not found.");
        if (order.Status != LabOrderStatus.SampleReceived)
            throw new InvalidLabOrderStateException("Results can only be entered after the sample is received.");

        var now = DateTime.UtcNow;
        order.Status = LabOrderStatus.Completed;
        order.ResultSummary = request.Summary.Trim();
        order.ResultEnteredAt = now;
        order.ResultEnteredByUserId = userId;
        order.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        await _events.PublishLabResultReadyAsync(order, userId, DateTime.UtcNow);
        return LabOrderResponse.FromEntity(order);
    }
}
