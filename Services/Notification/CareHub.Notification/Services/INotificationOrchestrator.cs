using CareHub.Notification.Models;

namespace CareHub.Notification.Services;

public interface INotificationOrchestrator
{
    Task HandleAsync(
        string dedupeKey,
        NotificationKind kind,
        IReadOnlyList<Guid> userIds,
        NotificationPayload payload,
        CancellationToken cancellationToken = default);
}
