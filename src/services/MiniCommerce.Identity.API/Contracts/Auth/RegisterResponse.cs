namespace MiniCommerce.Identity.API.Contracts.Auth;

public sealed record RegisterResponse(
    Guid Id,
    string Email,
    string FullName,
    DateTimeOffset CreatedAt);
