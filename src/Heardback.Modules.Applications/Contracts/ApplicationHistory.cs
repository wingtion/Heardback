namespace Heardback.Modules.Applications.Contracts;

/// <summary>What the insights need about one application; no posting text, notes or links.</summary>
/// <param name="ApplicationId">The application's id.</param>
/// <param name="Company">Company name.</param>
/// <param name="Role">Role or job title.</param>
/// <param name="Channel">How the user applied, by name, such as <c>LinkedInEasyApply</c>.</param>
/// <param name="AppliedOn">The applied date.</param>
/// <param name="LastActivityOn">The date of the latest timeline event.</param>
/// <param name="NoResponseDays">Days without a response when "No response" applies; otherwise null.</param>
/// <param name="StatusChanges">Status changes after the submission, in timeline order (date, then entry time).</param>
public sealed record ApplicationHistory(
    Guid ApplicationId,
    string Company,
    string Role,
    string Channel,
    DateOnly AppliedOn,
    DateOnly LastActivityOn,
    int? NoResponseDays,
    IReadOnlyList<ApplicationStatusChange> StatusChanges);
