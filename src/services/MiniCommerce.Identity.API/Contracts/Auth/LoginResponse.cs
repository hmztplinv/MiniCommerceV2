namespace MiniCommerce.Identity.API.Contracts.Auth;

public sealed record LoginResponse(
    Guid Id,
    string Email,
    string FullName,
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
