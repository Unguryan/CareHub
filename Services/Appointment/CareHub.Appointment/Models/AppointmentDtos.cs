namespace CareHub.Appointment.Models;

public record CreateAppointmentRequest(
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId,
    DateTime ScheduledAt,
    int DurationMinutes = 30);

public record RescheduleAppointmentRequest(
    DateTime ScheduledAt,
    int? DurationMinutes);

public record CancelAppointmentRequest(string Reason);

public record CompleteAppointmentRequest(bool RequiresLabWork);

public record AppointmentResponse(
    Guid Id,
    Guid PatientId,
    Guid DoctorId,
    Guid BranchId,
    DateTime ScheduledAt,
    int DurationMinutes,
    AppointmentStatus Status,
    bool RequiresLabWork,
    DateTime? CheckedInAt,
    DateTime? CompletedAt,
    string? CancellationReason,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static AppointmentResponse FromEntity(Appointment a) =>
        new(
            a.Id,
            a.PatientId,
            a.DoctorId,
            a.BranchId,
            a.ScheduledAt,
            a.DurationMinutes,
            a.Status,
            a.RequiresLabWork,
            a.CheckedInAt,
            a.CompletedAt,
            a.CancellationReason,
            a.CreatedAt,
            a.UpdatedAt);
}

// JSON shape must match Schedule Service (camelCase, DateOnly / TimeOnly).
public record ScheduleValidateSlotRequest(Guid DoctorId, DateOnly Date, TimeOnly SlotTime);

public record ScheduleValidateSlotResponse(bool IsValid, string? Reason);
