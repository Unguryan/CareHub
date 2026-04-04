using CareHub.Identity.Models;
using CareHub.Shared.Contracts.Events.Identity;
using MassTransit;

namespace CareHub.Identity.Events;

public class UserEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public UserEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public Task PublishUserLoggedInAsync(ApplicationUser user, string[] roles)
        => _publishEndpoint.Publish(new UserLoggedIn(
            UserId: user.Id,
            PhoneNumber: user.PhoneNumber ?? user.UserName!,
            Roles: roles,
            BranchId: user.BranchId,
            OccurredAt: DateTime.UtcNow));

    public Task PublishUserLoggedOutAsync(Guid userId)
        => _publishEndpoint.Publish(new UserLoggedOut(
            UserId: userId,
            OccurredAt: DateTime.UtcNow));
}
