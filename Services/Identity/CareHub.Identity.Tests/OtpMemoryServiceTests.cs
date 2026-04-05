using CareHub.Identity.Services;
using FluentAssertions;
using Xunit;

namespace CareHub.Identity.Tests;

public class OtpMemoryServiceTests
{
    [Fact]
    public async Task Issue_and_validate_roundtrip()
    {
        var otp = new MemoryOtpService();
        var userId = Guid.NewGuid();
        var code = await otp.IssueOtpAsync(userId);
        code.Should().MatchRegex("^[0-9]{6}$");
        (await otp.TryValidateOtpAsync(userId, code)).Should().BeTrue();
        (await otp.TryValidateOtpAsync(userId, code)).Should().BeFalse();
    }

    [Fact]
    public async Task Lockout_after_failed_attempts()
    {
        var otp = new MemoryOtpService();
        var userId = Guid.NewGuid();
        await otp.IssueOtpAsync(userId);
        for (var i = 0; i < 5; i++)
            (await otp.TryValidateOtpAsync(userId, "000000")).Should().BeFalse();

        (await otp.IsLockedOutAsync(userId)).Should().BeTrue();
        await Assert.ThrowsAsync<InvalidOperationException>(() => otp.IssueOtpAsync(userId));
    }
}
