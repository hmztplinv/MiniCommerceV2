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

    private User(string email, string fullName)
    {
        Id = Guid.NewGuid();
        Email = NormalizeEmail(email);
        FullName = fullName.Trim();
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public static User Create(string email, string fullName)
    {
        return new User(email, fullName);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(passwordHash));
        }

        PasswordHash = passwordHash;
    }

    public static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
