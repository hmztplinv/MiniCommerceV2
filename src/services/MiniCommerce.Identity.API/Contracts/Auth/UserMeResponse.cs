namespace MiniCommerce.Identity.API.Contracts.Auth;

public sealed record UserMeResponse(
    Guid Id,
    string Email,
    string FullName,
    DateTimeOffset CreatedAt);
