using System.ComponentModel.DataAnnotations;

namespace Heardback.Modules.Identity.Authentication;

internal sealed class TokenSettings
{
    public const string SectionName = "Identity:Tokens";
    public const int MinimumSigningKeyBytes = 32;

    [Required]
    public string Issuer { get; set; } = "heardback-api";

    [Required]
    public string Audience { get; set; } = "heardback-web";

    /// <summary>Base64 HMAC-SHA256 key of at least 32 bytes. User-secrets locally, environment variable in production.</summary>
    [Required]
    public string SigningKey { get; set; } = string.Empty;

    [Range(typeof(TimeSpan), "00:01:00", "01:00:00")]
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Sliding: each refresh issues a token valid this long, capped by <see cref="AbsoluteSessionLifetime"/>.</summary>
    [Range(typeof(TimeSpan), "01:00:00", "90.00:00:00")]
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>Hard limit from sign-in: after this the user must sign in again, however active they are.</summary>
    [Range(typeof(TimeSpan), "01:00:00", "365.00:00:00")]
    public TimeSpan AbsoluteSessionLifetime { get; set; } = TimeSpan.FromDays(60);

    /// <summary>
    /// How long a just-rotated refresh token may be presented again without being treated as theft,
    /// so two tabs refreshing at the same moment do not log the user out everywhere.
    /// </summary>
    [Range(typeof(TimeSpan), "00:00:00", "00:05:00")]
    public TimeSpan RefreshReuseGracePeriod { get; set; } = TimeSpan.FromSeconds(30);

    public byte[] GetSigningKeyBytes() => Convert.FromBase64String(SigningKey);

    public bool HasValidSigningKey()
    {
        var buffer = new byte[SigningKey.Length];
        return Convert.TryFromBase64String(SigningKey, buffer, out var bytesWritten)
            && bytesWritten >= MinimumSigningKeyBytes;
    }
}
