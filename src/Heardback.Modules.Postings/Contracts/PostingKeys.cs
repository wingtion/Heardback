using Heardback.Modules.Postings.Domain;

namespace Heardback.Modules.Postings.Contracts;

/// <summary>
/// The keys <see cref="IJobPostings"/> matches postings by, so another module can recognise its own duplicates
/// without depending on what other users submitted to the shared catalogue.
/// </summary>
public static class PostingKeys
{
    /// <summary>Length of a content hash (lower-case hex SHA-256).</summary>
    public const int ContentHashLength = PostingContentHash.Length;

    /// <summary>The comparison key of a link, or <see langword="null"/> when there is no valid http(s) link.</summary>
    /// <param name="url">The link as the user entered it.</param>
    /// <returns>At most <see cref="PostingLimits.UrlMaxLength"/> characters.</returns>
    public static string? NormalizeUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && PostingUrl.TryNormalize(url, out var key) ? key : null;

    /// <summary>The fingerprint of company, role and text, or <see langword="null"/> without posting text.</summary>
    /// <param name="company">Company name.</param>
    /// <param name="title">Role (job title).</param>
    /// <param name="text">The pasted job description, if any.</param>
    /// <returns><see cref="ContentHashLength"/> lower-case hex characters, or null.</returns>
    public static string? ContentHash(string company, string title, string? text) =>
        PostingContentHash.ComputeOrNull(company, title, text);
}
