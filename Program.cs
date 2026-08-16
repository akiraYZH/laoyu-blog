using laoyu_blog_backend.Data;
using Microsoft.EntityFrameworkCore;
using laoyu_blog_backend.Services;
using laoyu_blog_backend.Exceptions;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<SlugConflictExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddScoped<BlogPostService>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();


var app = builder.Build();

app.UseExceptionHandler();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
