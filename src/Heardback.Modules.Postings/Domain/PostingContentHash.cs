using System.Security.Cryptography;
using System.Text;

namespace Heardback.Modules.Postings.Domain;

/// <summary>
/// A fingerprint of company, role and posting text, to recognise the same job posted under different links.
/// Case, Unicode form and whitespace do not change it.
/// </summary>
internal static class PostingContentHash
{
    /// <summary>Length of the lower-case hex SHA-256 string.</summary>
    public const int Length = 64;

    // A control character that never appears in typed text, so "A" + "BC" and "AB" + "C" hash differently.
    private const char FieldSeparator = '\u001F';

    /// <summary>
    /// The fingerprint, or <see langword="null"/> without posting text: company and role alone are too common
    /// to say two submissions are the same posting.
    /// </summary>
    public static string? ComputeOrNull(string company, string title, string? text)
    {
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(title);

        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var canonical = string.Join(FieldSeparator, Canonicalize(company), Canonicalize(title), Canonicalize(text));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }

    private static string Canonicalize(string value) =>
        string.Join(' ', value.Normalize(NormalizationForm.FormC)
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
}
