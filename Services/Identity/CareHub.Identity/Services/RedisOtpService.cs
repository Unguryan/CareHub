using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace CareHub.Identity.Services;

public sealed class RedisOtpService(IConnectionMultiplexer redis) : IOtpService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockoutTtl = TimeSpan.FromMinutes(15);

    private IDatabase Db => redis.GetDatabase();

    public async Task<string> IssueOtpAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await IsLockedOutAsync(userId, cancellationToken))
            throw new InvalidOperationException("User is locked out.");

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var hash = Hash(code);
        await Db.StringSetAsync($"otp:{userId}", hash, OtpTtl);
        await Db.KeyDeleteAsync($"otp_attempts:{userId}");
        return code;
    }

    public async Task<bool> TryValidateOtpAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        if (await Db.KeyExistsAsync($"otp_lockout:{userId}"))
            return false;

        var stored = await Db.StringGetAsync($"otp:{userId}");
        if (stored.IsNullOrEmpty)
            return false;

        if (Hash(code) == stored.ToString())
        {
            await Db.KeyDeleteAsync($"otp:{userId}");
            await Db.KeyDeleteAsync($"otp_attempts:{userId}");
            return true;
        }

        var attempts = await Db.StringIncrementAsync($"otp_attempts:{userId}");
        await Db.KeyExpireAsync($"otp_attempts:{userId}", OtpTtl);
        if (attempts >= MaxFailedAttempts)
            await Db.StringSetAsync($"otp_lockout:{userId}", "1", LockoutTtl);

        return false;
    }

    public async Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Db.KeyExistsAsync($"otp_lockout:{userId}");

    private static string Hash(string plain)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(plain));
        return Convert.ToHexString(bytes);
    }
}
