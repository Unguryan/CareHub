using CareHub.Reporting.Data;
using CareHub.Reporting.Models;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Patients;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Reporting.Services;

public class ReportingProjectionService
{
    private readonly ReportingDbContext _db;
    private readonly ILogger<ReportingProjectionService> _log;

    public ReportingProjectionService(ReportingDbContext db, ILogger<ReportingProjectionService> log)
    {
        _db = db;
        _log = log;
    }

    public async Task ApplyPatientCreatedAsync(PatientCreated msg, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (await _db.ReportPatientFacts.AnyAsync(p => p.PatientId == msg.PatientId, ct))
        {
            _log.LogInformation("Skipping duplicate PatientCreated for {PatientId}", msg.PatientId);
            await tx.CommitAsync(ct);
            return;
        }

        _db.ReportPatientFacts.Add(new ReportPatientFact
        {
            PatientId = msg.PatientId,
            BranchId = msg.BranchId,
            CreatedAt = msg.OccurredAt
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyAppointmentCreatedAsync(AppointmentCreated msg, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var row = await _db.ReportAppointmentFacts.FindAsync([msg.AppointmentId], ct);
        if (row is null)
        {
            _db.ReportAppointmentFacts.Add(new ReportAppointmentFact
            {
                AppointmentId = msg.AppointmentId,
                PatientId = msg.PatientId,
                DoctorId = msg.DoctorId,
                BranchId = msg.BranchId,
                ScheduledAt = msg.ScheduledAt
            });
        }
        else
        {
            row.PatientId = msg.PatientId;
            row.DoctorId = msg.DoctorId;
            row.BranchId = msg.BranchId;
            row.ScheduledAt = msg.ScheduledAt;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyAppointmentCompletedAsync(AppointmentCompleted msg, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var row = await _db.ReportAppointmentFacts.FindAsync([msg.AppointmentId], ct);
        if (row is null)
        {
            _db.ReportAppointmentFacts.Add(new ReportAppointmentFact
            {
                AppointmentId = msg.AppointmentId,
                PatientId = msg.PatientId,
                DoctorId = msg.DoctorId,
                BranchId = msg.BranchId,
                ScheduledAt = msg.CompletedAt,
                CompletedAt = msg.CompletedAt
            });
        }
        else
        {
            row.PatientId = msg.PatientId;
            row.DoctorId = msg.DoctorId;
            row.BranchId ??= msg.BranchId;
            row.CompletedAt = msg.CompletedAt;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyAppointmentCancelledAsync(AppointmentCancelled msg, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        var row = await _db.ReportAppointmentFacts.FindAsync([msg.AppointmentId], ct);
        if (row is null)
        {
            _db.ReportAppointmentFacts.Add(new ReportAppointmentFact
            {
                AppointmentId = msg.AppointmentId,
                PatientId = msg.PatientId,
                DoctorId = msg.DoctorId,
                BranchId = null,
                ScheduledAt = msg.OccurredAt,
                CancelledAt = msg.OccurredAt,
                CancellationReason = msg.Reason
            });
        }
        else
        {
            row.PatientId = msg.PatientId;
            row.DoctorId = msg.DoctorId;
            row.CancelledAt = msg.OccurredAt;
            row.CancellationReason = msg.Reason;
        }

        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyPaymentCompletedAsync(
        PaymentCompleted msg,
        string dedupeKey,
        CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (await _db.ReportPaymentFacts.AnyAsync(p => p.MessageId == dedupeKey, ct))
        {
            _log.LogInformation("Skipping duplicate payment projection for message {MessageId}", dedupeKey);
            await tx.CommitAsync(ct);
            return;
        }

        _db.ReportPaymentFacts.Add(new ReportPaymentFact
        {
            Id = Guid.NewGuid(),
            MessageId = dedupeKey,
            InvoiceId = msg.InvoiceId,
            PatientId = msg.PatientId,
            BranchId = msg.BranchId,
            Amount = msg.Amount,
            Currency = msg.Currency,
            IsRefund = false,
            OccurredAt = msg.OccurredAt,
            ActorUserId = msg.ProcessedByUserId
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }

    public async Task ApplyRefundIssuedAsync(RefundIssued msg, string dedupeKey, CancellationToken ct)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);
        if (await _db.ReportPaymentFacts.AnyAsync(p => p.MessageId == dedupeKey, ct))
        {
            _log.LogInformation("Skipping duplicate refund projection for message {MessageId}", dedupeKey);
            await tx.CommitAsync(ct);
            return;
        }

        _db.ReportPaymentFacts.Add(new ReportPaymentFact
        {
            Id = Guid.NewGuid(),
            MessageId = dedupeKey,
            InvoiceId = msg.InvoiceId,
            PatientId = msg.PatientId,
            BranchId = msg.BranchId,
            Amount = -msg.Amount,
            Currency = msg.Currency,
            IsRefund = true,
            OccurredAt = msg.OccurredAt,
            ActorUserId = msg.IssuedByUserId
        });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
