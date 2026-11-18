using Heardback.Modules.Applications.Domain;

namespace Heardback.Modules.Applications.Responses;

/// <summary>
/// One application with its timeline, newest event first. <see cref="Version"/> goes back with an edit so a
/// change made elsewhere in the meantime is detected.
/// </summary>
internal sealed record ApplicationResponse(
    Guid Id,
    string Company,
    string Role,
    string? Url,
    string? PostingText,
    ApplicationChannel Channel,
    DateOnly AppliedOn,
    ApplicationStatus Status,
    int? NoResponseDays,
    string? Notes,
    DateTimeOffset UpdatedAt,
    uint Version,
    IReadOnlyList<ApplicationEventResponse> Events)
{
    public static ApplicationResponse From(Application application, NoResponseClock noResponse) =>
        new(
            application.Id,
            application.Company,
            application.Role,
            application.Url,
            application.PostingText,
            application.Channel,
            application.AppliedOn,
            application.Status,
            noResponse.DaysFor(application.Status, application.LastActivityOn),
            application.Notes,
            application.UpdatedAt,
            application.Version,
            application.Events
                .OrderByDescending(timelineEvent => timelineEvent.OccurredOn)
                .ThenByDescending(timelineEvent => timelineEvent.CreatedAt)
                .Select(timelineEvent => new ApplicationEventResponse(
                    timelineEvent.Id,
                    timelineEvent.Status,
                    timelineEvent.OccurredOn,
                    timelineEvent.Note,
                    timelineEvent.IsSubmission))
                .ToList());
}
