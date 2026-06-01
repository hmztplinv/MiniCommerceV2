namespace MiniCommerce.Identity.API.Contracts.Auth;

public sealed record LoginRequest(
    string Email,
    string Password);
