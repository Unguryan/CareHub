namespace CareHub.Notification.Models;

public enum NotificationKind
{
    AppointmentCreated,
    AppointmentCancelled,
    AppointmentRescheduled,
    InvoiceGenerated,
    LabResultReady
}

public record NotificationPayload(
    string Title,
    string Body,
    DateTime OccurredAt,
    string? EntityType = null,
    string? EntityId = null);

public record ClientNotificationDto(
    string Kind,
    string Title,
    string Body,
    DateTime OccurredAt,
    string? EntityType,
    string? EntityId);
