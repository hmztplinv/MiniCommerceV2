namespace MiniCommerce.Identity.API.Contracts.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string FullName);
