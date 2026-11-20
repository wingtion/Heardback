namespace Heardback.Modules.Applications.Contracts;

/// <summary>The signed-in user's applications and the community groups that passed the minimum size.</summary>
/// <param name="Applications">The user's own applications, newest applied first.</param>
/// <param name="Postings">Outcomes per shared posting id, for postings the user tracks.</param>
/// <param name="Companies">Outcomes per company key, for companies the user tracks.</param>
public sealed record CommunitySnapshot(
    IReadOnlyList<TrackedApplication> Applications,
    IReadOnlyDictionary<Guid, PostingOutcome> Postings,
    IReadOnlyDictionary<string, CompanyOutcome> Companies);
