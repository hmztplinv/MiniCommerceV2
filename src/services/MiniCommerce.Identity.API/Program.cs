using Microsoft.EntityFrameworkCore;
using MiniCommerce.Identity.API.Options;
using MiniCommerce.Identity.API.Persistence;

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

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    service = "MiniCommerce.Identity.API",
    status = "Healthy",
    timestamp = DateTimeOffset.UtcNow
}));

app.Run();
