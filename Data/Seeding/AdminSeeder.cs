using laoyu_blog_backend.Models;
using Microsoft.AspNetCore.Identity;

namespace laoyu_blog_backend.Data.Seeding;

public static class AdminSeeder
{
    private const string AdminRole = "Admin";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];

        if (string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin email and password are not configured.");
        }

        using var scope = services.CreateScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(AdminRole))
        {
            var roleResult = await roleManager.CreateAsync(
                new IdentityRole(AdminRole));

            EnsureSucceeded(roleResult, "Creating Admin role");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var userResult = await userManager.CreateAsync(
                admin,
                password);

            EnsureSucceeded(userResult, "Creating Admin user");
        }

        if (!await userManager.IsInRoleAsync(admin, AdminRole))
        {
            var roleResult = await userManager.AddToRoleAsync(
                admin,
                AdminRole);

            EnsureSucceeded(roleResult, "Assigning Admin role");
        }
    }

    private static void EnsureSucceeded(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => error.Description));

        throw new InvalidOperationException(
            $"{operation} failed: {errors}");
    }
}