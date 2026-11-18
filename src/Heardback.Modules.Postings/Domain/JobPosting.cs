namespace Heardback.Modules.Postings.Domain;

/// <summary>
/// One job posting, shared by every user who tracks it. Its content is written once and never edited, so
/// no user can change what others' applications point to (ADR-0004). It holds no user id.
/// </summary>
internal sealed class JobPosting
{
    private JobPosting()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// The first submitted link without credentials, tracking parameters or fragment, or null when the posting was
    /// added without one. The raw link can carry details about the person who copied it.
    /// </summary>
    public string? Url { get; private set; }

    /// <summary>Comparison key from <see cref="PostingUrl"/>; unique.</summary>
    public string? NormalizedUrl { get; private set; }

    public string Company { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    /// <summary>The posting text as first submitted, kept for later analysis.</summary>
    public string? Text { get; private set; }

    /// <summary>Fingerprint from <see cref="PostingContentHash"/>; unique; null without text.</summary>
    public string? ContentHash { get; private set; }

    public DateTimeOffset FirstSeenAt { get; private set; }

    public DateTimeOffset LastSeenAt { get; private set; }

    /// <summary>How many times users have submitted this posting, the first time included.</summary>
    public int TimesAdded { get; private set; }

    public static JobPosting Create(
        PostingUrlParts? link,
        string company,
        string title,
        string? text,
        string? contentHash,
        DateTimeOffset now) =>
        new()
        {
            Id = Guid.CreateVersion7(now),
            Url = link?.CleanUrl,
            NormalizedUrl = link?.Key,
            Company = company.Trim(),
            Title = title.Trim(),
            Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim(),
            ContentHash = contentHash,
            FirstSeenAt = now,
            LastSeenAt = now,
            TimesAdded = 1,
        };
}
