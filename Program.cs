using System.IdentityModel.Tokens.Jwt;
using System.Text;
using laoyu_blog_backend.Options;
using laoyu_blog_backend.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using laoyu_blog_backend.Data;
using Microsoft.AspNetCore.Identity;
using laoyu_blog_backend.Models;
using Microsoft.EntityFrameworkCore;
using laoyu_blog_backend.Services;
using laoyu_blog_backend.Exceptions;
using laoyu_blog_backend.Data.Seeding;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("LaoyuBlog")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

var jwtOptions = builder.Configuration
    .GetSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException(
        "JWT configuration was not found.");

if (string.IsNullOrWhiteSpace(jwtOptions.Issuer)
    || string.IsNullOrWhiteSpace(jwtOptions.Audience)
    || Encoding.UTF8.GetByteCount(jwtOptions.Key) < 32
    || jwtOptions.ExpirationMinutes <= 0)
{
    throw new InvalidOperationException(
        "JWT configuration is invalid.");
}

builder.Services.AddSingleton(jwtOptions);
builder.Services.AddSingleton<JwtTokenService>();

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,

                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,

                ValidateLifetime = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtOptions.Key)),

                NameClaimType =
                    JwtRegisteredClaimNames.UniqueName,

                RoleClaimType = "role",

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SlugConflictExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddScoped<BlogPostService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<IImageStorageService, LocalImageStorageService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();



var app = builder.Build();

await AdminSeeder.SeedAsync(
    app.Services,
    app.Configuration);

app.UseExceptionHandler();

// use static files for image uploads
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

public partial class Program
{
}