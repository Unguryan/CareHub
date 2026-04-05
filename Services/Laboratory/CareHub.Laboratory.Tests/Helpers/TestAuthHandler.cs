using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CareHub.Laboratory.Tests.Helpers;

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
            new Claim("sub", LaboratoryTestFactory.DefaultUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, LaboratoryTestFactory.DefaultUserId.ToString()),
            new Claim("branch_id", LaboratoryTestFactory.DefaultBranchId.ToString()),
            new Claim(ClaimTypes.Role, "LabTechnician"),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim(ClaimTypes.Role, "Auditor"),
            new Claim(ClaimTypes.Role, "Doctor"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), "Test");
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
