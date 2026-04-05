using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace CareHub.Identity.Services;

/// <summary>In-memory OTP store for automated tests (ASPNETCORE_ENVIRONMENT=Testing).</summary>
public sealed class MemoryOtpService : IOtpService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan OtpTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan LockoutTtl = TimeSpan.FromMinutes(15);

    private sealed class Entry
    {
        public string CodeHash = "";
        public DateTime ExpiresAt;
        public int FailedAttempts;
        public DateTime? LockoutUntil;
    }

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();

    public Task<string> IssueOtpAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var entry = _entries.GetOrAdd(userId, _ => new Entry());
        lock (entry)
        {
            if (entry.LockoutUntil is { } until && until > DateTime.UtcNow)
                throw new InvalidOperationException("User is locked out.");

            var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
            entry.CodeHash = Hash(code);
            entry.ExpiresAt = DateTime.UtcNow + OtpTtl;
            entry.FailedAttempts = 0;
            return Task.FromResult(code);
        }
    }

    public Task<bool> TryValidateOtpAsync(Guid userId, string code, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(userId, out var entry))
            return Task.FromResult(false);

        lock (entry)
        {
            if (entry.LockoutUntil is { } until && until > DateTime.UtcNow)
                return Task.FromResult(false);

            if (string.IsNullOrEmpty(entry.CodeHash) || DateTime.UtcNow > entry.ExpiresAt)
                return Task.FromResult(false);

            if (Hash(code) == entry.CodeHash)
            {
                entry.CodeHash = "";
                entry.FailedAttempts = 0;
                entry.LockoutUntil = null;
                return Task.FromResult(true);
            }

            entry.FailedAttempts++;
            if (entry.FailedAttempts >= MaxFailedAttempts)
                entry.LockoutUntil = DateTime.UtcNow + LockoutTtl;

            return Task.FromResult(false);
        }
    }

    public Task<bool> IsLockedOutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_entries.TryGetValue(userId, out var entry))
            return Task.FromResult(false);

        lock (entry)
        {
            return Task.FromResult(entry.LockoutUntil is { } until && until > DateTime.UtcNow);
        }
    }

    private static string Hash(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }
}
