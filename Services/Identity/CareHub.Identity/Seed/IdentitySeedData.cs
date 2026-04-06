using CareHub.Identity.Models;
using CareHub.Shared.AspNetCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CareHub.Identity.Seed;

public static class IdentitySeedData
{
    public static readonly string[] AllRoles =
    [
        "Admin", "Doctor", "Manager", "Receptionist", "CallCenter",
        "LabTechnician", "Accountant", "Auditor"
    ];

    private static readonly Guid DefaultBranchId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await SeedInfrastructureAsync(services);
        if (!configuration.SeedDemoData())
            return;

        await SeedDemoUsersAsync(services);
    }

    private static async Task SeedInfrastructureAsync(IServiceProvider services)
    {
        await SeedRolesAsync(services);
        await SeedOidcClientsAsync(services);
    }

    private static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in AllRoles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));
        }
    }

    private static async Task SeedDemoUsersAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        (string Phone, string Password, string Role)[] accounts =
        [
            ("+380000000000", "Admin1234!", "Admin"),
            ("+380000000001", "Doctor1234!", "Doctor"),
            ("+380000000002", "User1234!", "Receptionist"),
            ("+380000000003", "Manager1234!", "Manager"),
            ("+380000000004", "Lab1234!", "LabTechnician"),
        ];

        foreach (var (phone, password, role) in accounts)
            await EnsureUserAsync(userManager, phone, password, role, DefaultBranchId);
    }

    private static async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string phone,
        string password,
        string role,
        Guid branchId)
    {
        if (await userManager.FindByNameAsync(phone) is not null)
            return;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = phone,
            PhoneNumber = phone,
            BranchId = branchId,
            PhoneNumberConfirmed = true,
        };

        await userManager.CreateAsync(user, password);
        await userManager.AddToRoleAsync(user, role);
    }

    private static async Task SeedOidcClientsAsync(IServiceProvider services)
    {
        var manager = services.GetRequiredService<IOpenIddictApplicationManager>();

        // Desktop client — uses password grant
        if (await manager.FindByClientIdAsync("carehub-desktop") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "carehub-desktop",
                ClientSecret = "desktop-secret",
                DisplayName = "CareHub Desktop Client",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.Scope + "openid",
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                }
            });
        }

        // Service-to-service — uses client credentials
        if (await manager.FindByClientIdAsync("carehub-services") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "carehub-services",
                ClientSecret = "services-secret",
                DisplayName = "CareHub Internal Services",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.ClientCredentials,
                    Permissions.Prefixes.Scope + "api",
                }
            });
        }

        if (await manager.FindByClientIdAsync("carehub-web") is null)
        {
            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "carehub-web",
                ClientSecret = "web-secret",
                DisplayName = "CareHub Web Portal (dev)",
                Permissions =
                {
                    Permissions.Endpoints.Token,
                    Permissions.GrantTypes.Password,
                    Permissions.GrantTypes.RefreshToken,
                    Permissions.Prefixes.Scope + "openid",
                    Permissions.Scopes.Profile,
                    Permissions.Prefixes.Scope + "offline_access",
                    Permissions.Prefixes.Scope + "api",
                }
            });
        }
    }
}
