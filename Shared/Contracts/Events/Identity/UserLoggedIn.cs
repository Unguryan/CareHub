namespace CareHub.Shared.Contracts.Events.Identity;

public record UserLoggedIn(
    Guid UserId,
    string PhoneNumber,
    string[] Roles,
    Guid BranchId,
    DateTime OccurredAt
);
