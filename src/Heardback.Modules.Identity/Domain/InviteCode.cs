using System.Linq.Expressions;
using System.Security.Cryptography;

namespace Heardback.Modules.Identity.Domain;

/// <summary>A single-use code that lets one person register.</summary>
internal sealed class InviteCode
{
    public const int CodeLength = 12;

    /// <summary>Longest accepted input: room for separators and spacing, and for a longer bootstrap code.</summary>
    public const int MaxInputLength = 64;
    private const int GroupLength = 4;
    private const char GroupSeparator = '-';

    // Crockford base32: no I, L, O or U, so codes survive being read aloud or retyped.
    private const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    private InviteCode()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Normalized form: upper case, no separators.</summary>
    public string Code { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset ExpiresAt { get; private set; }

    public Guid? UsedByUserId { get; private set; }

    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>The code as shown to people, e.g. <c>K7QX-M2PA-9TDF</c>.</summary>
    public string DisplayCode => Format(Code);

    public static InviteCode Generate(Guid createdByUserId, DateTimeOffset now, TimeSpan lifetime) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            Code = RandomNumberGenerator.GetString(Alphabet, CodeLength),
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now + lifetime,
        };

    /// <summary>Accepts any casing, separators and surrounding whitespace.</summary>
    public static string Normalize(string input) =>
        string.Concat(input.Where(character => character != GroupSeparator && !char.IsWhiteSpace(character)))
            .ToUpperInvariant();

    /// <summary>True when a normalized code has the shape of a generated one: 12 characters from the alphabet.</summary>
    public static bool IsWellFormed(string normalizedCode) =>
        normalizedCode.Length == CodeLength && normalizedCode.All(character => Alphabet.Contains(character, StringComparison.Ordinal));

    public static string Format(string normalizedCode) =>
        string.Join(GroupSeparator, normalizedCode.Chunk(GroupLength).Select(group => new string(group)));

    public InviteCodeStatus GetStatus(DateTimeOffset now) => StatusAt(UsedAt, ExpiresAt, now);

    /// <summary>
    /// Status from raw fields, for read queries that project instead of loading the entity. "Used" is decided
    /// by <see cref="UsedAt"/>, which is never cleared; <see cref="UsedByUserId"/> becomes null if that account
    /// is deleted, and must not bring the code back to life.
    /// </summary>
    public static InviteCodeStatus StatusAt(DateTimeOffset? usedAt, DateTimeOffset expiresAt, DateTimeOffset now) =>
        usedAt is not null ? InviteCodeStatus.Used
        : now >= expiresAt ? InviteCodeStatus.Expired
        : InviteCodeStatus.Active;

    /// <summary>
    /// The <see cref="InviteCodeStatus.Active"/> rule of <see cref="StatusAt"/> as a query filter, so
    /// counts can run in the database. The two must stay in step.
    /// </summary>
    public static Expression<Func<InviteCode, bool>> IsActiveAt(DateTimeOffset now) =>
        invite => invite.UsedAt == null && invite.ExpiresAt > now;
}
