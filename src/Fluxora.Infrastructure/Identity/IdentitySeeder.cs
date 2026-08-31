using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fluxora.Infrastructure.Identity;

/// <summary>
/// Seeds the fixed role set on startup, and optionally a bootstrap Admin user - only when
/// Bootstrap:AdminEmail / Bootstrap:AdminPassword are supplied via configuration/environment.
/// No default password ever ships in code.
/// </summary>
public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }

        var configuration = services.GetRequiredService<IConfiguration>();
        var adminEmail = configuration["Bootstrap:AdminEmail"];
        var adminPassword = configuration["Bootstrap:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByEmailAsync(adminEmail) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            DisplayName = "Administrator",
            EmailConfirmed = true,
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        }
        else
        {
            var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
            logger.LogWarning(
                "Bootstrap admin user could not be created: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
