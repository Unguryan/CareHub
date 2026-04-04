using System.Security.Claims;
using CareHub.Identity.Events;
using CareHub.Identity.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CareHub.Identity.Auth;

public class PasswordGrantHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    public static OpenIddictServerHandlerDescriptor Descriptor { get; }
        = OpenIddictServerHandlerDescriptor
            .CreateBuilder<OpenIddictServerEvents.HandleTokenRequestContext>()
            .UseScopedHandler<PasswordGrantHandler>()
            .SetOrder(500)
            .SetType(OpenIddictServerHandlerType.Custom)
            .Build();

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserEventPublisher _eventPublisher;

    public PasswordGrantHandler(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        UserEventPublisher eventPublisher)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _eventPublisher = eventPublisher;
    }

    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType()) return;

        var user = await _userManager.FindByNameAsync(context.Request.Username!);
        if (user is null || !await _userManager.CheckPasswordAsync(user, context.Request.Password!))
        {
            context.Reject(
                error: Errors.InvalidGrant,
                description: "Phone number or password is incorrect.");
            return;
        }

        var roles = await _userManager.GetRolesAsync(user);

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.SetClaim(Claims.Subject, user.Id.ToString())
                .SetClaim(Claims.Name, user.UserName!)
                .SetClaims(Claims.Role, [.. roles])
                .SetClaim("branch_id", user.BranchId.ToString());

        identity.SetScopes(context.Request.GetScopes());
        identity.SetDestinations(GetDestinations);

        await _eventPublisher.PublishUserLoggedInAsync(user, [.. roles]);

        context.SignIn(new ClaimsPrincipal(identity));
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        return claim.Type switch
        {
            Claims.Name or Claims.Role or "branch_id"
                => [Destinations.AccessToken, Destinations.IdentityToken],
            _ => [Destinations.AccessToken]
        };
    }
}
