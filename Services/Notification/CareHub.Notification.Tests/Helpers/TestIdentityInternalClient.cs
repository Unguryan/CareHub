using CareHub.Notification.Clients;

namespace CareHub.Notification.Tests.Helpers;

public sealed class TestIdentityInternalClient : IIdentityInternalClient
{
    public long? ChatIdToReturn { get; set; } = 1000;

    public List<IdentityUserTelegramRow> BranchRoleResult { get; set; } =
    [
        new(Guid.Parse("aaaaaaaa-bbbb-bbbb-bbbb-aaaaaaaaaaa1"), 2001L),
        new(Guid.Parse("aaaaaaaa-bbbb-bbbb-bbbb-aaaaaaaaaaa2"), 2002L)
    ];

    public Task<long?> GetTelegramChatIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        foreach (var row in BranchRoleResult)
        {
            if (row.UserId == userId)
                return Task.FromResult(row.TelegramChatId);
        }

        return Task.FromResult(ChatIdToReturn);
    }

    public Task<IReadOnlyList<IdentityUserTelegramRow>> GetUsersByBranchAndRoleAsync(
        Guid branchId,
        string role,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<IdentityUserTelegramRow>>(BranchRoleResult);
}
