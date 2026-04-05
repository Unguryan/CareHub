using CareHub.Notification.Hubs;
using CareHub.Notification.Models;
using Microsoft.AspNetCore.SignalR;

namespace CareHub.Notification.Messaging;

public sealed class SignalRNotificationPublisher(IHubContext<NotificationHub> hub)
{
    public Task PublishToUserAsync(Guid userId, ClientNotificationDto dto, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(NotificationHub.UserGroupName(userId))
            .SendAsync("ReceiveNotification", dto, cancellationToken);
}
