namespace Heardback.Modules.Insights.FollowUps;

/// <summary>An application that shows "No response · N days" and may be worth a follow-up.</summary>
/// <param name="ApplicationId">The application's id.</param>
/// <param name="Company">Company name.</param>
/// <param name="Role">Role or job title.</param>
/// <param name="Channel">How the user applied, by name.</param>
/// <param name="AppliedOn">The applied date.</param>
/// <param name="LastActivityOn">The latest timeline event; adding a note moves it and takes the item off the list.</param>
/// <param name="NoResponseDays">Days since the last event.</param>
internal sealed record FollowUpItem(
    Guid ApplicationId,
    string Company,
    string Role,
    string Channel,
    DateOnly AppliedOn,
    DateOnly LastActivityOn,
    int NoResponseDays);
