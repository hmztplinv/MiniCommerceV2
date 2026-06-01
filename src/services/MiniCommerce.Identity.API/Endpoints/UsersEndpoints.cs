using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.Identity.API.Contracts.Auth;
using MiniCommerce.Identity.API.Persistence;

namespace MiniCommerce.Identity.API.Endpoints;

public static class UsersEndpoints
{
    public static RouteGroupBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization();

        group.MapGet("/me", GetCurrentUserAsync);

        return group;
    }

    private static async Task<IResult> GetCurrentUserAsync(
        ClaimsPrincipal claimsPrincipal,
        IdentityDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userIdClaim = claimsPrincipal.FindFirstValue("userId");

        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return Results.Unauthorized();
        }

        if (!Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);

        if (user is null)
        {
            return Results.NotFound(new
            {
                message = "User was not found."
            });
        }

        var response = new UserMeResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt);

        return Results.Ok(response);
    }
}
