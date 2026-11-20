namespace Heardback.Modules.Applications.Contracts;

/// <summary>A posting group that passed the minimum size.</summary>
/// <param name="Outcome">Other members' settled applications to the posting.</param>
/// <param name="FirstSeenAt">When anyone first added the posting; only given once the group passed the minimum size.</param>
public sealed record PostingOutcome(CommunityOutcome Outcome, DateTimeOffset FirstSeenAt);
