using System.Security.Cryptography;
using System.Text;
using Heardback.Shared.Persistence;

namespace Heardback.Modules.Identity.Domain;

/// <summary>
/// A one-time code that lets one user set a new password (ADR-0015). It has the shape of an invite code, is stored
/// only as a SHA-256 hash, expires after <see cref="Lifetime"/> and works once.
/// </summary>
[NotUserOwned("Looked up by code hash by an anonymous request, and issued by admins for other users.")]
internal sealed class PasswordResetCode
{
    public const int HashLength = 64;

    public static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    private PasswordResetCode()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>Hex SHA-256 of the normalized code; unique. The code itself is shown once and never stored.</summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>The admin who issued it, or null when the owner issued it on the server.</summary>
    public Guid? IssuedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Creates a code for <paramref name="userId"/>; the returned plain code is the only copy.</summary>
    public static (PasswordResetCode Code, string PlainCode) Issue(Guid userId, Guid? issuedByUserId, DateTimeOffset now)
    {
        var plain = InviteCode.GenerateCodeText();
        var code = new PasswordResetCode
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            CodeHash = Hash(plain),
            IssuedByUserId = issuedByUserId,
            CreatedAt = now,
            ExpiresAt = now + Lifetime,
        };
        return (code, InviteCode.Format(plain));
    }

    /// <summary>Hash of a code as typed: casing, dashes and spaces do not matter.</summary>
    public static string Hash(string input) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(InviteCode.Normalize(input))));

    public static bool IsWellFormed(string input) => InviteCode.IsWellFormed(InviteCode.Normalize(input));
}
