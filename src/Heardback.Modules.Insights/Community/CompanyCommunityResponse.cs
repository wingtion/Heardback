namespace Heardback.Modules.Insights.Community;

/// <summary>
/// The company page: other members' numbers and the viewer's own applications there. Never other members'
/// postings, roles or dates (ADR-0013).
/// </summary>
/// <param name="CompanyKey">The company's community group.</param>
/// <param name="Company">The name as the viewer last typed it.</param>
/// <param name="Numbers">Other members' numbers; null when fewer than the minimum applied.</param>
/// <param name="Applications">The viewer's own applications at this company, newest applied first.</param>
internal sealed record CompanyCommunityResponse(
    string CompanyKey,
    string Company,
    CommunityNumbers? Numbers,
    IReadOnlyList<CompanyApplicationItem> Applications);
