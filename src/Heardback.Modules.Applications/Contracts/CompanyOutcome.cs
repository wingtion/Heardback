namespace Heardback.Modules.Applications.Contracts;

/// <summary>A company group that passed the minimum size, with how often each tracked role was posted.</summary>
/// <param name="Outcome">Other members' settled applications at the company.</param>
/// <param name="PostingsPerRole">
/// For each role key the user tracks at this company: how many separate shared postings members (the user
/// included) have linked for that role. A role posted again and again is one ghost-listing hint.
/// </param>
public sealed record CompanyOutcome(CommunityOutcome Outcome, IReadOnlyDictionary<string, int> PostingsPerRole);
