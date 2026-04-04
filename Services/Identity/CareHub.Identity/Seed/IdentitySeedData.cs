using CareHub.Identity.Models;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace CareHub.Identity.Seed;

public static class IdentitySeedData
{
    public static readonly string[] AllRoles =
    [
        "Admin", "Doctor", "Receptionist", "CallCenter",
        "LabTechnician", "Accountant", "Auditor"
    ];

    public static async Task SeedAsync(IServiceProvider services)
    {
        await SeedRolesAsync(services);
        await SeedAdminUserAsync(services);
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

    private static async Task SeedAdminUserAsync(IServiceProvider services)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        const string adminPhone = "+380000000000";

        if (await userManager.FindByNameAsync(adminPhone) is not null) return;

        var admin = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = adminPhone,
            PhoneNumber = adminPhone,
            BranchId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            PhoneNumberConfirmed = true,
        };

        await userManager.CreateAsync(admin, "Admin1234!");
        await userManager.AddToRoleAsync(admin, "Admin");
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
    }
}
