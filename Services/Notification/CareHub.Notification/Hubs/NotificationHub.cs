using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CareHub.Notification.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    public static string UserGroupName(Guid userId) => $"user:{userId}";

    public Task Join()
    {
        var sub = Context.User?.FindFirst("sub")?.Value;
        if (sub is null || !Guid.TryParse(sub, out var userId))
            throw new HubException("Missing or invalid subject claim.");

        return Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));
    }
}
