using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareHub.Document.Tests.Helpers;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var claims = new[]
        {
            new Claim("sub", DocumentTestFactory.DefaultUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, DocumentTestFactory.DefaultUserId.ToString()),
            new Claim("branch_id", DocumentTestFactory.DefaultBranchId.ToString()),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Receptionist"),
            new Claim(ClaimTypes.Role, "Accountant"),
            new Claim(ClaimTypes.Role, "Doctor"),
            new Claim(ClaimTypes.Role, "LabTechnician"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
