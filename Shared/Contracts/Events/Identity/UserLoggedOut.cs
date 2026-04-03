namespace CareHub.Shared.Contracts.Events.Identity;

public record UserLoggedOut(
    Guid UserId,
    DateTime OccurredAt
);
