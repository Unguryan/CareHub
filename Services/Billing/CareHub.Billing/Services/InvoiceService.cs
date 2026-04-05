using CareHub.Billing.Data;
using CareHub.Billing.Events;
using CareHub.Billing.Exceptions;
using CareHub.Billing.Models;
using CareHub.Shared.Contracts.Events.Appointments;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Billing.Services;

public class InvoiceService
{
    private readonly BillingDbContext _db;
    private readonly BillingEventPublisher _events;
    private readonly IConfiguration _config;

    public InvoiceService(BillingDbContext db, BillingEventPublisher events, IConfiguration config)
    {
        _db = db;
        _events = events;
        _config = config;
    }

    public async Task CreateFromAppointmentCompletedAsync(AppointmentCompleted msg, CancellationToken ct = default)
    {
        if (await _db.Invoices.AnyAsync(i => i.AppointmentId == msg.AppointmentId, ct))
            return;

        var amount = _config.GetValue<decimal>("Billing:DefaultConsultationAmount");
        var currency = _config["Billing:Currency"] ?? "UAH";
        var now = DateTime.UtcNow;
        var invoice = new Invoice
        {
            Id = Guid.NewGuid(),
            AppointmentId = msg.AppointmentId,
            PatientId = msg.PatientId,
            BranchId = msg.BranchId,
            Amount = amount,
            Currency = currency,
            Status = InvoiceStatus.Unpaid,
            CreatedAt = now,
            UpdatedAt = now,
        };

        _db.Invoices.Add(invoice);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            if (await _db.Invoices.AnyAsync(i => i.AppointmentId == msg.AppointmentId, ct))
                return;
            throw;
        }

        await _events.PublishInvoiceGeneratedAsync(invoice, DateTime.UtcNow);
    }

    public async Task<List<InvoiceResponse>> ListAsync(
        Guid? patientId,
        Guid? branchId,
        InvoiceStatus? status,
        DateTime? fromUtc,
        DateTime? toUtc,
        bool global,
        Guid callerBranchId,
        CancellationToken ct = default)
    {
        var q = _db.Invoices.AsQueryable();

        if (!global && branchId is null)
            q = q.Where(i => i.BranchId == callerBranchId);
        else if (branchId.HasValue)
            q = q.Where(i => i.BranchId == branchId.Value);

        if (patientId.HasValue) q = q.Where(i => i.PatientId == patientId.Value);
        if (status.HasValue) q = q.Where(i => i.Status == status.Value);
        if (fromUtc.HasValue) q = q.Where(i => i.CreatedAt >= fromUtc.Value);
        if (toUtc.HasValue) q = q.Where(i => i.CreatedAt <= toUtc.Value);

        var rows = await q.OrderByDescending(i => i.CreatedAt).ToListAsync(ct);
        return rows.ConvertAll(InvoiceResponse.FromEntity);
    }

    public async Task<InvoiceResponse?> GetAsync(Guid id, bool global, Guid callerBranchId, CancellationToken ct)
    {
        var i = await _db.Invoices.FindAsync([id], ct);
        if (i is null) return null;
        if (!global && i.BranchId != callerBranchId) return null;
        return InvoiceResponse.FromEntity(i);
    }

    public async Task<InvoiceResponse> MarkPaidAsync(Guid id, Guid userId, Guid callerBranchId, CancellationToken ct = default)
    {
        var invoice = await _db.Invoices.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");
        if (invoice.BranchId != callerBranchId)
            throw new KeyNotFoundException($"Invoice {id} not found.");
        if (invoice.Status != InvoiceStatus.Unpaid)
            throw new InvalidInvoiceStateException("Only unpaid invoices can be marked paid.");

        var now = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Paid;
        invoice.PaidAt = now;
        invoice.PaidByUserId = userId;
        invoice.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        await _events.PublishPaymentCompletedAsync(invoice, userId, DateTime.UtcNow);
        return InvoiceResponse.FromEntity(invoice);
    }

    public async Task<InvoiceResponse> RefundAsync(
        Guid id,
        RefundInvoiceRequest request,
        Guid userId,
        Guid callerBranchId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Refund reason is required.", nameof(request));

        var invoice = await _db.Invoices.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"Invoice {id} not found.");
        if (invoice.BranchId != callerBranchId)
            throw new KeyNotFoundException($"Invoice {id} not found.");
        if (invoice.Status != InvoiceStatus.Paid)
            throw new InvalidInvoiceStateException("Only paid invoices can be refunded.");

        var now = DateTime.UtcNow;
        invoice.Status = InvoiceStatus.Refunded;
        invoice.RefundedAt = now;
        invoice.RefundedByUserId = userId;
        invoice.RefundReason = request.Reason.Trim();
        invoice.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
        await _events.PublishRefundIssuedAsync(invoice, userId, invoice.RefundReason, DateTime.UtcNow);
        return InvoiceResponse.FromEntity(invoice);
    }
}
