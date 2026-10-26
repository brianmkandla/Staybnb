using Microsoft.AspNetCore.Identity;
using System.Linq;
using Staybnb.Models;

namespace Staybnb.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(
            RoleManager<IdentityRole> roleManager, 
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration)
        {
            string[] roles =
            {
                "Guest", 
                "Host",
                "Admin",
                "SuperAdmin"
            };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(
                            "; ",
                            result.Errors.Select(error => error.Description));

                        throw new InvalidOperationException(
                            $"Failed to create role '{role}': {errors}");
                    }
                }
            }


            var superAdminEmail = configuration["SeedAdmin:Email"] 
                ?? "admin@staybnb.com";
            var superAdminPassword = configuration["SeedAdmin:Password"];

            var superAdmin = await userManager.FindByEmailAsync(superAdminEmail);

            if (superAdmin == null)
            {
                if (string.IsNullOrWhiteSpace(superAdminPassword))
                {
                    throw new InvalidOperationException(
                        "Configure SeedAdmin:Password before creating the SuperAdmin.");
                }

                superAdmin = new ApplicationUser
                {
                    UserName = superAdminEmail,
                    Email = superAdminEmail,
                    EmailConfirmed = true,
                    FirstName = "Super",
                    LastName = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(superAdmin, superAdminPassword);

                if (!result.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        result.Errors.Select(error => error.Description));

                    throw new InvalidOperationException(
                        $"SuperAdmin creation failed: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(superAdmin, "SuperAdmin"))
            {
                var roleResult = await userManager.AddToRoleAsync(superAdmin, "SuperAdmin");

                if (!roleResult.Succeeded)
                {
                    var errors = string.Join(
                        "; ",
                        roleResult.Errors.Select(error => error.Description));

                    throw new InvalidOperationException(
                        $"SuperAdmin role assignment failed: {errors}");
                }
            }

        }
    }
}
