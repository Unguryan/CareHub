namespace CareHub.Reporting.Models.Reports.V1;

public record VisitVolumeRow(
    DateOnly Period,
    Guid BranchId,
    Guid DoctorId,
    int ScheduledCount,
    int CompletedCount);

public record VisitsReportResponse(IReadOnlyList<VisitVolumeRow> Rows, bool Truncated);

public record RevenueRow(DateOnly Period, Guid BranchId, decimal NetAmount, string Currency);

public record RevenueReportResponse(IReadOnlyList<RevenueRow> Rows, bool Truncated);

public record WorkloadRow(Guid BranchId, Guid DoctorId, int ScheduledCount, int CompletedCount);

public record WorkloadReportResponse(IReadOnlyList<WorkloadRow> Rows, bool Truncated);

public record CancellationRow(
    Guid BranchId,
    Guid DoctorId,
    string Reason,
    int CancelledCount,
    int ScheduledInPeriodCount,
    double CancellationRate);

public record CancellationsReportResponse(IReadOnlyList<CancellationRow> Rows, bool Truncated);
