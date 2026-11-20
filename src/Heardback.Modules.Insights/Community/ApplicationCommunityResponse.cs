namespace Heardback.Modules.Insights.Community;

/// <summary>What the community shows for one of the viewer's applications.</summary>
/// <param name="ApplicationId">The viewer's application.</param>
/// <param name="CompanyKey">Opens the company page; null on rows saved before company keys existed.</param>
/// <param name="Posting">Other members' numbers for this posting; null when fewer than the minimum applied.</param>
/// <param name="Company">Other members' numbers for this company; null when fewer than the minimum applied.</param>
/// <param name="Ghost">The ghost-listing hint; null when neither group has enough members.</param>
internal sealed record ApplicationCommunityResponse(
    Guid ApplicationId,
    string? CompanyKey,
    CommunityNumbers? Posting,
    CommunityNumbers? Company,
    GhostSignal? Ghost);
