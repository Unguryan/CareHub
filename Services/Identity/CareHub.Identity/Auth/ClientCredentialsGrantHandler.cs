using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CareHub.Identity.Auth;

public class ClientCredentialsGrantHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor
            .CreateBuilder<OpenIddictServerEvents.HandleTokenRequestContext>()
            .UseSingletonHandler<ClientCredentialsGrantHandler>()
            .SetOrder(400)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    public ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsClientCredentialsGrantType()) return ValueTask.CompletedTask;

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, context.Request.ClientId!);
        identity.SetScopes(context.Request.GetScopes());
        identity.SetDestinations(_ => [Destinations.AccessToken]);

        context.SignIn(new ClaimsPrincipal(identity));
        return ValueTask.CompletedTask;
    }
}
