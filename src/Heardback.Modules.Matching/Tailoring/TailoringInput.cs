namespace Heardback.Modules.Matching.Tailoring;

/// <summary>What the tailoring prompt reads.</summary>
/// <param name="CvText">The stored CV text; personal details were removed on upload (ADR-0006).</param>
/// <param name="Company">The application's company.</param>
/// <param name="Role">The application's role.</param>
/// <param name="PostingText">The application's posting text.</param>
/// <param name="MatchedSkills">Skills the deterministic match found in both.</param>
/// <param name="MissingSkills">Skills the posting names that the CV does not show.</param>
internal sealed record TailoringInput(
    string CvText,
    string Company,
    string Role,
    string PostingText,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills);
