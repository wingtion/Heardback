namespace Heardback.Modules.Identity.Domain;

internal sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;

    private User()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>The email as the user typed it (trimmed), for display.</summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>Case-insensitive lookup key; unique.</summary>
    public string NormalizedEmail { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public bool IsAdmin { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// When the password last changed after registration, or null. Access tokens issued before this second are
    /// rejected, so a reset or change also ends sessions whose access token has not expired yet (ADR-0015).
    /// </summary>
    public DateTimeOffset? PasswordChangedAt { get; private set; }

    public static User Create(string email, string displayName, bool isAdmin, DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            Email = email.Trim(),
            NormalizedEmail = NormalizeEmail(email),
            DisplayName = displayName.Trim(),
            IsAdmin = isAdmin,
            CreatedAt = now,
        };

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>Replaces the password; the caller also ends the user's sessions.</summary>
    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = now;
    }
}
