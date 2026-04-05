using System.Text.Json;
using CareHub.Audit.Data;
using CareHub.Audit.Models;
using CareHub.Shared.Contracts.Events.Appointments;
using CareHub.Shared.Contracts.Events.Billing;
using CareHub.Shared.Contracts.Events.Identity;
using CareHub.Shared.Contracts.Events.Patients;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CareHub.Audit.Services;

public class AuditLogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AuditDbContext _db;
    private readonly ILogger<AuditLogWriter> _logger;

    public AuditLogWriter(AuditDbContext db, ILogger<AuditLogWriter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Task WriteUserLoggedInAsync(UserLoggedIn e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "identity.user.logged_in",
            e.UserId,
            entityType: null,
            entityId: null,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteUserLoggedOutAsync(UserLoggedOut e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "identity.user.logged_out",
            e.UserId,
            null,
            null,
            null,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WritePatientCreatedAsync(PatientCreated e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "patient.created",
            e.CreatedByUserId,
            "Patient",
            e.PatientId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WritePatientUpdatedAsync(PatientUpdated e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "patient.updated",
            e.UpdatedByUserId,
            "Patient",
            e.PatientId,
            null,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteAppointmentCreatedAsync(AppointmentCreated e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "appointment.created",
            e.CreatedByUserId,
            "Appointment",
            e.AppointmentId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteAppointmentCancelledAsync(AppointmentCancelled e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "appointment.cancelled",
            e.CancelledByUserId,
            "Appointment",
            e.AppointmentId,
            null,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteAppointmentRescheduledAsync(AppointmentRescheduled e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "appointment.rescheduled",
            e.RescheduledByUserId,
            "Appointment",
            e.AppointmentId,
            null,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteAppointmentCompletedAsync(AppointmentCompleted e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "appointment.completed",
            e.CompletedByUserId,
            "Appointment",
            e.AppointmentId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteInvoiceGeneratedAsync(InvoiceGenerated e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "billing.invoice.generated",
            actorUserId: null,
            "Invoice",
            e.InvoiceId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WritePaymentCompletedAsync(PaymentCompleted e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "billing.payment.completed",
            e.ProcessedByUserId,
            "Invoice",
            e.InvoiceId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    public Task WriteRefundIssuedAsync(RefundIssued e, string? brokerMessageId, CancellationToken ct) =>
        AppendAsync(
            e.OccurredAt,
            "billing.refund.issued",
            e.IssuedByUserId,
            "Invoice",
            e.InvoiceId,
            e.BranchId,
            JsonSerializer.Serialize(e, JsonOptions),
            brokerMessageId,
            ct);

    private async Task AppendAsync(
        DateTime recordedAt,
        string actionType,
        Guid? actorUserId,
        string? entityType,
        Guid? entityId,
        Guid? branchId,
        string detailsJson,
        string? brokerMessageId,
        CancellationToken ct)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            RecordedAt = DateTime.SpecifyKind(recordedAt, DateTimeKind.Utc),
            ActionType = actionType,
            ActorUserId = actorUserId,
            EntityType = entityType,
            EntityId = entityId,
            BranchId = branchId,
            DetailsJson = detailsJson,
            BrokerMessageId = brokerMessageId
        };

        _db.AuditLogEntries.Add(entry);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicateBrokerMessageViolation(ex))
        {
            _logger.LogDebug(ex, "Duplicate broker message id {BrokerMessageId}, skipping insert", brokerMessageId);
        }
    }

    private static bool IsDuplicateBrokerMessageViolation(DbUpdateException ex)
    {
        for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
        {
            if (inner is PostgresException pg && pg.SqlState == PostgresErrorCodes.UniqueViolation)
                return true;
            var msg = inner.Message;
            if (msg.Contains("IX_AuditLogEntries_BrokerMessageId", StringComparison.OrdinalIgnoreCase))
                return true;
            if (msg.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
                && msg.Contains("BrokerMessageId", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
