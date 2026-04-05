namespace CareHub.Notification.Models;

public class NotificationDelivery
{
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public required string Kind { get; set; }

    public required string Channel { get; set; }

    public Guid TargetUserId { get; set; }

    public bool Success { get; set; }

    public string? ErrorMessage { get; set; }

    public string? PayloadSummary { get; set; }

    public required string DedupeKey { get; set; }
}
