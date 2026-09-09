using laoyu_blog_backend.Data;
using laoyu_blog_backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace LaoyuBlog.Api.Tests;

public sealed class BlogApiFactory
    : WebApplicationFactory<Program>
{
    private readonly string _databaseName =
    $"LaoyuBlogTests-{Guid.NewGuid()}";

    private static readonly Dictionary<string, string?>
        TestSettings = new()
        {
            ["ConnectionStrings:LaoyuBlog"] =
                "Host=unused",

            ["Admin:Email"] =
                "admin@test.local",

            ["Admin:Password"] =
                "TestAdmin123!",

            ["Jwt:Issuer"] =
                "laoyu-blog-tests",

            ["Jwt:Audience"] =
                "laoyu-blog-tests",

            ["Jwt:Key"] =
                "test-signing-key-at-least-32-bytes-long",

            ["Jwt:ExpirationMinutes"] =
                "60"
        };

    protected override IHost CreateHost(
        IHostBuilder builder)
    {
        builder.ConfigureHostConfiguration(
            configuration =>
            {
                configuration.AddInMemoryCollection(
                    TestSettings);
            });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<AppDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(
                options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });
        });
    }


    public async Task CreateUserWithoutRoleAsync(
        string email,
        string password)
    {
        using var scope = Services.CreateScope();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var result = await userManager.CreateAsync(
            new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            },
            password);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                "; ",
                result.Errors.Select(error => error.Description));

            throw new InvalidOperationException(
                $"Creating test user failed: {errors}");
        }
    }
}
