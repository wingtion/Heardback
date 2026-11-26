namespace Heardback.Modules.Identity.Domain;

internal sealed class User
{
    public const int EmailMaxLength = 254;
    public const int DisplayNameMaxLength = 100;

    /// <summary>
    /// Demo accounts get addresses here. The .invalid top-level domain can never receive mail or belong to anyone,
    /// and registration refuses it, so a real account can never collide with one.
    /// </summary>
    public const string DemoEmailDomain = "demo.invalid";

    private const string DemoVisitorName = "Demo visitor";
    private const string DemoNeighborName = "Demo neighbor";

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

    public UserKind Kind { get; private set; }

    public bool IsDemo => Kind != UserKind.Member;

    /// <summary>When a demo visitor is removed; null for every other account, which never expires.</summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

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

    /// <summary>A "Try the demo" sandbox. It has no password, so nobody can sign in to it again.</summary>
    public static User CreateDemoVisitor(DateTimeOffset now, TimeSpan lifetime)
    {
        var id = Guid.CreateVersion7(now);
        var visitor = CreateDemo(id, $"visitor-{id:N}@{DemoEmailDomain}", DemoVisitorName, UserKind.DemoVisitor, now);
        visitor.ExpiresAt = now + lifetime;
        return visitor;
    }

    /// <summary>One of the fixed made-up accounts behind the demo's community signals.</summary>
    public static User CreateDemoNeighbor(int number, DateTimeOffset now) =>
        CreateDemo(
            Guid.CreateVersion7(now),
            DemoNeighborEmail(number),
            $"{DemoNeighborName} {number}",
            UserKind.DemoNeighbor,
            now);

    public static string DemoNeighborEmail(int number) => $"neighbor-{number}@{DemoEmailDomain}";

    public static bool IsDemoEmail(string email) =>
        email.Trim().EndsWith($"@{DemoEmailDomain}", StringComparison.OrdinalIgnoreCase);

    public static string NormalizeEmail(string email) => email.Trim().ToUpperInvariant();

    private static User CreateDemo(Guid id, string email, string displayName, UserKind kind, DateTimeOffset now) =>
        new()
        {
            Id = id,
            Email = email,
            NormalizedEmail = NormalizeEmail(email),
            DisplayName = displayName,
            Kind = kind,
            CreatedAt = now,
        };

    public void SetPasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>Replaces the password; the caller also ends the user's sessions.</summary>
    public void ChangePassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        PasswordChangedAt = now;
    }
}
