using CareHub.Reporting.Data;
using CareHub.Reporting.Models;
using CareHub.Reporting.Models.Reports.V1;
using Microsoft.EntityFrameworkCore;

namespace CareHub.Reporting.Services;

public class ReportQueryService
{
    private const int DefaultMaxRows = 500;

    private readonly ReportingDbContext _db;

    public ReportQueryService(ReportingDbContext db) => _db = db;

    public async Task<VisitsReportResponse> GetVisitsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? filterBranchId,
        Guid? doctorId,
        int maxRows,
        CancellationToken ct)
    {
        maxRows = Math.Clamp(maxRows, 1, DefaultMaxRows);

        var q = _db.ReportAppointmentFacts.AsNoTracking();
        if (filterBranchId.HasValue)
            q = q.Where(a => a.BranchId == filterBranchId.Value);
        if (doctorId.HasValue)
            q = q.Where(a => a.DoctorId == doctorId.Value);

        var rows = await q.ToListAsync(ct);

        var acc = new Dictionary<(DateOnly Period, Guid BranchId, Guid DoctorId), (int Sched, int Comp)>();

        foreach (var a in rows)
        {
            if (a.BranchId is not { } branchId)
                continue;

            if (a.ScheduledAt >= fromUtc && a.ScheduledAt <= toUtc)
            {
                var period = DateOnly.FromDateTime(UtcDate(a.ScheduledAt).Date);
                var key = (period, branchId, a.DoctorId);
                acc.TryGetValue(key, out var v);
                acc[key] = (v.Sched + 1, v.Comp);
            }

            if (a.CompletedAt is { } completedAt && completedAt >= fromUtc && completedAt <= toUtc)
            {
                var period = DateOnly.FromDateTime(UtcDate(completedAt).Date);
                var key = (period, branchId, a.DoctorId);
                acc.TryGetValue(key, out var v);
                acc[key] = (v.Sched, v.Comp + 1);
            }
        }

        var list = acc
            .Select(kv => new VisitVolumeRow(kv.Key.Period, kv.Key.BranchId, kv.Key.DoctorId, kv.Value.Sched, kv.Value.Comp))
            .OrderBy(r => r.Period)
            .ThenBy(r => r.BranchId)
            .ThenBy(r => r.DoctorId)
            .ToList();

        var truncated = list.Count > maxRows;
        if (truncated)
            list = list.Take(maxRows).ToList();

        return new VisitsReportResponse(list, truncated);
    }

    public async Task<RevenueReportResponse> GetRevenueAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? filterBranchId,
        int maxRows,
        CancellationToken ct)
    {
        maxRows = Math.Clamp(maxRows, 1, DefaultMaxRows);

        var q = _db.ReportPaymentFacts.AsNoTracking()
            .Where(p => p.OccurredAt >= fromUtc && p.OccurredAt <= toUtc);
        if (filterBranchId.HasValue)
            q = q.Where(p => p.BranchId == filterBranchId.Value);

        var groups = await q
            .GroupBy(p => new
            {
                Day = p.OccurredAt.Date,
                p.BranchId,
                p.Currency
            })
            .Select(g => new
            {
                g.Key.Day,
                g.Key.BranchId,
                g.Key.Currency,
                Net = g.Sum(x => x.Amount)
            })
            .OrderBy(x => x.Day)
            .ThenBy(x => x.BranchId)
            .ThenBy(x => x.Currency)
            .Take(maxRows + 1)
            .ToListAsync(ct);

        var truncated = groups.Count > maxRows;
        var slice = truncated ? groups.Take(maxRows).ToList() : groups;
        var rows = slice
            .Select(x => new RevenueRow(DateOnly.FromDateTime(x.Day), x.BranchId, x.Net, x.Currency))
            .ToList();

        return new RevenueReportResponse(rows, truncated);
    }

    public async Task<WorkloadReportResponse> GetWorkloadAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? filterBranchId,
        Guid? doctorId,
        int maxRows,
        CancellationToken ct)
    {
        maxRows = Math.Clamp(maxRows, 1, DefaultMaxRows);

        var q = _db.ReportAppointmentFacts.AsNoTracking();
        if (filterBranchId.HasValue)
            q = q.Where(a => a.BranchId == filterBranchId.Value);
        if (doctorId.HasValue)
            q = q.Where(a => a.DoctorId == doctorId.Value);

        var rows = await q.ToListAsync(ct);

        var acc = new Dictionary<(Guid BranchId, Guid DoctorId), (int Sched, int Comp)>();

        foreach (var a in rows)
        {
            if (a.BranchId is not { } branchId)
                continue;

            var key = (branchId, a.DoctorId);
            acc.TryGetValue(key, out var v);
            var sched = v.Sched;
            var comp = v.Comp;

            if (a.CancelledAt is null && a.ScheduledAt >= fromUtc && a.ScheduledAt <= toUtc)
                sched++;
            if (a.CompletedAt is { } c && c >= fromUtc && c <= toUtc)
                comp++;

            acc[key] = (sched, comp);
        }

        var list = acc
            .Select(kv => new WorkloadRow(kv.Key.BranchId, kv.Key.DoctorId, kv.Value.Sched, kv.Value.Comp))
            .Where(r => r.ScheduledCount > 0 || r.CompletedCount > 0)
            .OrderBy(r => r.BranchId)
            .ThenBy(r => r.DoctorId)
            .ToList();

        var truncated = list.Count > maxRows;
        if (truncated)
            list = list.Take(maxRows).ToList();

        return new WorkloadReportResponse(list, truncated);
    }

    public async Task<CancellationsReportResponse> GetCancellationsAsync(
        DateTime fromUtc,
        DateTime toUtc,
        Guid? filterBranchId,
        Guid? doctorId,
        int maxRows,
        CancellationToken ct)
    {
        maxRows = Math.Clamp(maxRows, 1, DefaultMaxRows);

        var q = _db.ReportAppointmentFacts.AsNoTracking();
        if (filterBranchId.HasValue)
            q = q.Where(a => a.BranchId == filterBranchId.Value);
        if (doctorId.HasValue)
            q = q.Where(a => a.DoctorId == doctorId.Value);

        var rows = await q.ToListAsync(ct);

        var scheduledDenoms = new Dictionary<(Guid BranchId, Guid DoctorId), int>();
        foreach (var a in rows)
        {
            if (a.BranchId is not { } branchId)
                continue;
            if (a.ScheduledAt >= fromUtc && a.ScheduledAt <= toUtc)
            {
                var key = (branchId, a.DoctorId);
                scheduledDenoms[key] = scheduledDenoms.GetValueOrDefault(key) + 1;
            }
        }

        var cancelGroups = new Dictionary<(Guid BranchId, Guid DoctorId, string Reason), int>();
        foreach (var a in rows)
        {
            if (a.BranchId is not { } branchId)
                continue;
            if (a.CancelledAt is not { } ca || ca < fromUtc || ca > toUtc)
                continue;
            var reason = string.IsNullOrEmpty(a.CancellationReason) ? "(none)" : a.CancellationReason!;
            var key = (branchId, a.DoctorId, reason);
            cancelGroups[key] = cancelGroups.GetValueOrDefault(key) + 1;
        }

        var list = cancelGroups
            .Select(kv =>
            {
                var (branchId, docId, reason) = kv.Key;
                var denom = scheduledDenoms.GetValueOrDefault((branchId, docId));
                var rate = denom > 0 ? (double)kv.Value / denom : 0d;
                return new CancellationRow(branchId, docId, reason, kv.Value, denom, rate);
            })
            .OrderByDescending(r => r.CancelledCount)
            .ThenBy(r => r.BranchId)
            .ThenBy(r => r.DoctorId)
            .ToList();

        var truncated = list.Count > maxRows;
        if (truncated)
            list = list.Take(maxRows).ToList();

        return new CancellationsReportResponse(list, truncated);
    }

    private static DateTime UtcDate(DateTime dt) =>
        dt.Kind == DateTimeKind.Utc ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Utc);
}
