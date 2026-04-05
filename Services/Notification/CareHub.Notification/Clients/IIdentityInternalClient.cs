namespace CareHub.Notification.Clients;

public interface IIdentityInternalClient
{
    Task<long?> GetTelegramChatIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IdentityUserTelegramRow>> GetUsersByBranchAndRoleAsync(
        Guid branchId,
        string role,
        CancellationToken cancellationToken = default);
}
