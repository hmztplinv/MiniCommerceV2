using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.Identity.API.Endpoints;
using MiniCommerce.Identity.API.Entities;
using MiniCommerce.Identity.API.Options;
using MiniCommerce.Identity.API.Persistence;
using MiniCommerce.Identity.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection(JwtOptions.SectionName));

builder.Services.AddDbContext<IdentityDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("IdentityDb");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException("Connection string 'IdentityDb' is not configured.");
    }

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<PasswordHasher<User>>();
builder.Services.AddScoped<JwtTokenService>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "MiniCommerce.Identity.API",
    status = "Healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.MapAuthEndpoints();

app.Run();
