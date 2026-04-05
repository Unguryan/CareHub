namespace CareHub.Appointment.Models;

public class Appointment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid BranchId { get; set; }

    /// <summary>UTC start instant of the booked slot.</summary>
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;

    public string? CancellationReason { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public DateTime? CancelledAt { get; set; }

    public DateTime? CheckedInAt { get; set; }

    /// <summary>Set when appointment is completed; copied into AppointmentCompleted event.</summary>
    public bool RequiresLabWork { get; set; }

    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByUserId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid CreatedByUserId { get; set; }
}
