using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareHub.Reporting.Tests.Helpers;

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
            ? new[] { "Manager" }
            : roleHeader.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var claims = new List<Claim>
        {
            new("sub", ReportingTestFactory.DefaultUserId.ToString()),
            new(ClaimTypes.NameIdentifier, ReportingTestFactory.DefaultUserId.ToString()),
            new("branch_id", ReportingTestFactory.DefaultBranchId.ToString())
        };
        foreach (var r in roleNames)
            claims.Add(new Claim(ClaimTypes.Role, r));

        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
