using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareHub.Audit.Tests.Helpers;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string AnonymousHeader = "none";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (Request.Headers.TryGetValue("X-Test-Auth", out var auth) &&
            auth.ToString().Equals(AnonymousHeader, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.Fail("Test anonymous"));

        var roleHeader = Request.Headers["X-Test-Roles"].FirstOrDefault();
        var roleNames = string.IsNullOrEmpty(roleHeader)
            ? new[] { "Admin", "Auditor" }
            : roleHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new("sub", AuditTestFactory.DefaultUserId.ToString()),
            new(ClaimTypes.NameIdentifier, AuditTestFactory.DefaultUserId.ToString()),
            new("branch_id", AuditTestFactory.DefaultBranchId.ToString())
        };
        foreach (var r in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
