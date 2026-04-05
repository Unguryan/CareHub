namespace CareHub.Notification.Models;

public class NotificationDedupe
{
    public Guid Id { get; set; }

    /// <summary>Deterministic key per domain event instance (e.g. AppointmentCreated:guid).</summary>
    public required string DedupeKey { get; set; }

    public DateTime CreatedAt { get; set; }
}
