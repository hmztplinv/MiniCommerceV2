using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.Identity.API.Contracts.Auth;
using MiniCommerce.Identity.API.Entities;
using MiniCommerce.Identity.API.Persistence;

namespace MiniCommerce.Identity.API.Endpoints;

public static class AuthEndpoints
{
    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync);

        return group;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IdentityDbContext dbContext,
        PasswordHasher<User> passwordHasher,
        CancellationToken cancellationToken)
    {
        var validationErrors = ValidateRegisterRequest(request);

        if (validationErrors.Count > 0)
        {
            return Results.BadRequest(new
            {
                message = "Validation failed.",
                errors = validationErrors
            });
        }

        var normalizedEmail = User.NormalizeEmail(request.Email);

        var emailAlreadyExists = await dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail, cancellationToken);

        if (emailAlreadyExists)
        {
            return Results.Conflict(new
            {
                message = "Email is already registered."
            });
        }

        var user = User.Create(normalizedEmail, request.FullName);

        var passwordHash = passwordHasher.HashPassword(user, request.Password);

        user.SetPasswordHash(passwordHash);

        dbContext.Users.Add(user);

        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.CreatedAt);

        return Results.Ok(response);
    }

    private static List<string> ValidateRegisterRequest(RegisterRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!request.Email.Contains('@', StringComparison.Ordinal))
        {
            errors.Add("Email format is invalid.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            errors.Add("Password is required.");
        }
        else if (request.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters.");
        }

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            errors.Add("Full name is required.");
        }

        return errors;
    }
}
