using Heardback.Modules.Applications.Contracts;

namespace Heardback.Modules.Matching.Tailoring;

/// <summary>What tailoring reads for one application, as it is right now.</summary>
internal sealed record TailoringSource(
    ApplicationPosting Posting,
    string? CvText,
    bool HasConsent,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills)
{
    public bool HasPostingText => !string.IsNullOrWhiteSpace(Posting.PostingText);

    public string PostingFingerprint => TailoringFingerprint.OfPosting(Posting.Company, Posting.Role, Posting.PostingText);

    public string? CvFingerprint => CvText is null ? null : TailoringFingerprint.OfCv(CvText);

    public TailoringInput ToInput() =>
        new(CvText!, Posting.Company, Posting.Role, Posting.PostingText!, MatchedSkills, MissingSkills);
}
