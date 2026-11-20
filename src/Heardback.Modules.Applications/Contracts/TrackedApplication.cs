namespace Heardback.Modules.Applications.Contracts;

/// <summary>One of the signed-in user's own applications, as the community signals need it.</summary>
/// <param name="ApplicationId">The application's id.</param>
/// <param name="PostingId">The shared posting it is linked to.</param>
/// <param name="Company">Company name as the user typed it.</param>
/// <param name="CompanyKey">The company's community group, or null on rows saved before keys existed.</param>
/// <param name="Role">Role as the user typed it.</param>
/// <param name="RoleKey">Comparison key of the role, or null on rows saved before keys existed.</param>
/// <param name="Stage">Current status.</param>
/// <param name="AppliedOn">The applied date.</param>
/// <param name="NoResponseDays">Days without a response when "No response" applies; otherwise null.</param>
/// <param name="UpdatedAt">When the user last changed anything, timeline included ("Last update").</param>
public sealed record TrackedApplication(
    Guid ApplicationId,
    Guid PostingId,
    string Company,
    string? CompanyKey,
    string Role,
    string? RoleKey,
    ApplicationStage Stage,
    DateOnly AppliedOn,
    int? NoResponseDays,
    DateTimeOffset UpdatedAt);
