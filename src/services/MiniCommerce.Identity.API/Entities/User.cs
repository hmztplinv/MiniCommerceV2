namespace MiniCommerce.Identity.API.Entities;

public sealed class User
{
    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    public User(string email, string passwordHash, string fullName)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
