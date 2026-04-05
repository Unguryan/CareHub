namespace CareHub.Identity.Services;

public interface IOtpService
{
    Task<string> IssueOtpAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> TryValidateOtpAsync(Guid userId, string code, CancellationToken cancellationToken = default);

    Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default);
}
