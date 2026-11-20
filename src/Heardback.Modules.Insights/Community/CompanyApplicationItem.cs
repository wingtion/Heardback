namespace Heardback.Modules.Insights.Community;

/// <summary>One of the viewer's own applications on the company page.</summary>
/// <param name="ApplicationId">Opens the application.</param>
/// <param name="Role">Role as the viewer typed it.</param>
/// <param name="Status">Current status by name, such as <c>Interview</c>.</param>
/// <param name="AppliedOn">The applied date.</param>
/// <param name="NoResponseDays">Days without a response when "No response" applies; otherwise null.</param>
/// <param name="UpdatedAt">When the viewer last changed it ("Last update").</param>
/// <param name="Ghost">Ghost-risk level of its posting; null when there is not enough community data.</param>
internal sealed record CompanyApplicationItem(
    Guid ApplicationId,
    string Role,
    string Status,
    DateOnly AppliedOn,
    int? NoResponseDays,
    DateTimeOffset UpdatedAt,
    GhostLevel? Ghost);
