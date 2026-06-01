using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MiniCommerce.Identity.API.Entities;
using MiniCommerce.Identity.API.Options;

namespace MiniCommerce.Identity.API.Services;

public sealed class JwtTokenService(IOptions<JwtOptions> jwtOptions)
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public (string AccessToken, DateTimeOffset ExpiresAt) GenerateToken(User user)
    {
        ValidateOptions();

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwtOptions.ExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("userId", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("fullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));

        var credentials = new SigningCredentials(
            securityKey,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

        return (accessToken, expiresAt);
    }

    private void ValidateOptions()
    {
        if (string.IsNullOrWhiteSpace(_jwtOptions.Issuer))
        {
            throw new InvalidOperationException("Jwt Issuer is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.Audience))
        {
            throw new InvalidOperationException("Jwt Audience is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_jwtOptions.SecretKey))
        {
            throw new InvalidOperationException("Jwt SecretKey is not configured.");
        }

        if (_jwtOptions.SecretKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt SecretKey must be at least 32 characters.");
        }

        if (_jwtOptions.ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("Jwt ExpirationMinutes must be greater than zero.");
        }
    }
}
