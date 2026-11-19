using System.Security.Cryptography;
using System.Text;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>
/// Fingerprints of the CV and posting a suggestion was made from; when either changes, the suggestion is shown
/// as out of date.
/// </summary>
internal static class TailoringFingerprint
{
    /// <summary>Length of the lower-case hex SHA-256 string.</summary>
    public const int Length = 64;

    public static string OfCv(string cvText) => Hash(cvText);

    public static string OfPosting(string company, string role, string? postingText) =>
        Hash(string.Join('\n', company, role, postingText ?? string.Empty));

    private static string Hash(string text) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
