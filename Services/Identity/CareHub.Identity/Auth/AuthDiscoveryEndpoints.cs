namespace CareHub.Identity.Auth;

public static class AuthDiscoveryEndpoints
{
    public static void MapAuthDiscoveryEndpoints(this WebApplication app)
    {
        var g = app.MapGroup("/api/auth").AllowAnonymous();
        g.MapGet("/login", () =>
            Results.Ok(new
            {
                message = "Use the OAuth2 token endpoint exposed via the gateway.",
                tokenEndpoint = "/connect/token",
                grantTypes = new[] { "password", "refresh_token" },
            }));
    }
}
